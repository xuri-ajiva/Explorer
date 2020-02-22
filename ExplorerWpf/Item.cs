#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using ExplorerWpf.Handler;

#endregion

namespace ExplorerWpf {
    public static class DefaultIcons {

        private const uint SHGFI_ICON      = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;

        // ReSharper disable once UnusedMember.Local
        private const uint SHGFI_SMALLICON = 0x000000001;

        private static Bitmap _errorCash;
        private static Icon   _errorCashI;

        private static Bitmap _shieldCash;

        private static          Bitmap                   _warningCash;
        private static readonly Dictionary<string, Icon> Cache = new Dictionary<string, Icon>();
        public static           Bitmap                   ErrorIcon   => _errorCash   ?? ( _errorCash = SystemIcons.Error.ToBitmap() );
        public static           Icon                     ErrorIconI  => _errorCashI  ?? ( _errorCashI = SystemIcons.Error );
        public static           Bitmap                   ShieldIcon  => _shieldCash  ?? ( _shieldCash = SystemIcons.Shield.ToBitmap() );
        public static           Bitmap                   WarningIcon => _warningCash ?? ( _warningCash = SystemIcons.Warning.ToBitmap() );


        public static Icon GetFileIconCashed(string path) {
            var ext = path;

            if ( File.Exists( path ) ) ext = Path.GetExtension( path );

            if ( SettingsHandler.ExtenstionWithSpecialIcons.Contains( ext ) ) ext = path;

            if ( ext == null )
                return null;

            Icon icon;
            if ( Cache.TryGetValue( ext, out icon ) )
                return icon;

            icon = ExtractFromPath( path );

            if ( icon == null ) return null;

            Cache.Add( ext, icon );
            return icon;
        }

        private static Icon ExtractFromPath(string path) {
            var shinfo = new SHFILEINFO();
            SHGetFileInfo( path, 0, ref shinfo, (uint) Marshal.SizeOf( shinfo ), SHGFI_ICON | SHGFI_LARGEICON );

            if ( shinfo.hIcon == IntPtr.Zero ) return default;

            return Icon.FromHandle( shinfo.hIcon );
        }

        [DllImport( "shell32.dll" )]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        //Struct used by SHGetFileInfo function
        [StructLayout( LayoutKind.Sequential )]
        // ReSharper disable once InconsistentNaming
        private struct SHFILEINFO {
            public readonly IntPtr hIcon;
            public readonly int    iIcon;
            public readonly uint   dwAttributes;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
            public readonly string szDisplayName;

            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
            public readonly string szTypeName;
        }
    }

    public class TreePathItem : Item {

        public TreePathItem(DirectoryInfo d) : base( d ) { Init( false ); }
        public TreePathItem(FileInfo      f) : base( f ) { Init( false ); }

        // ReSharper disable once RedundantBaseConstructorCall
        private TreePathItem() : base() { Init( true ); }

        public new static TreePathItem Empty => new TreePathItem();

        public ObservableCollection<TreePathItem> Items { get; set; }

        private void Init(bool noSubNode) {
            this.Items = new ObservableCollection<TreePathItem>();
            if ( !noSubNode )
                this.Items.Add( Empty );
        }
    }

    public class Item {
        public enum FileType {
            DIRECTORY, FILE, NONE
        }

        public Item(FileInfo f) {
            this.Path           = f.FullName.Replace( "\\\\", "\\" );
            this.Name           = f.Name;
            this.Size           = f.Length + $" ({GetLength( f.Length )})";
            this.Exists         = f.Exists;
            this.IsReadOnly     = f.IsReadOnly;
            this.Extension      = f.Extension;
            this.CreationTime   = f.CreationTime;
            this.LastAccessTime = f.LastAccessTime;
            this.LastWriteTime  = f.LastWriteTime;

            this.Type = FileType.FILE;

            CreateIcon();
            this.TryGetFileInfo = f;
        }

        public Item(DirectoryInfo d) {
            this.Path           = d.FullName.Replace( "\\\\", "\\" );
            this.Name           = d.Name;
            this.Size           = "0";
            this.Exists         = d.Exists;
            this.IsReadOnly     = false;
            this.Extension      = d.Extension;
            this.CreationTime   = d.CreationTime;
            this.LastAccessTime = d.LastAccessTime;
            this.LastWriteTime  = d.LastWriteTime;

            this.Type = FileType.DIRECTORY;

            CreateIcon();
            this.TryGetDirectoryInfo = d;
        }

        protected Item() {
            this.Icon           = DefaultIcons.ErrorIcon;
            this.Name           = "empty";
            this.Path           = "empty";
            this.Size           = "-1";
            this.Type           = FileType.NONE;
            this.Exists         = false;
            this.IsReadOnly     = true;
            this.Extension      = "*";
            this.CreationTime   = DateTime.MaxValue;
            this.LastAccessTime = DateTime.MaxValue;
            this.LastWriteTime  = DateTime.MaxValue;
        }

        public static Item Empty => new Item();

        public static Item Root {
            get {
                var i = new Item { Path = SettingsHandler.ROOT_FOLDER, Name = SettingsHandler.ROOT_FOLDER, Type = FileType.DIRECTORY, Size = long.MaxValue + $" ({GetLength( long.MaxValue )})" };
                i.Icon.Dispose();
                i.Icon = DefaultIcons.ShieldIcon;
                return i;
            }
        }


        [DebuggerStepThrough]
        public static string GetLength(long length) {
            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "0.00" ) + "Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "0.00" ) + "Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "0.00" )   + "Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "0.00" )   + "Mb";
            if ( length > Math.Pow( 10, 3 ) ) return ( length / Math.Pow( 10, 3 ) ).ToString( "0.00" )   + "Kb";

            return length + "b";
        }

        private void CreateIcon() {
            try {
                this.Icon = this.Path != SettingsHandler.ROOT_FOLDER ? DefaultIcons.GetFileIconCashed( this.Path ).ToBitmap() : DefaultIcons.ErrorIcon;
                if ( this.Icon == null ) this.Icon = DefaultIcons.WarningIcon;
            } catch (Exception e) {
                try {
                    this.Icon = DefaultIcons.ErrorIcon;
                } catch (Exception exception) {
                    SettingsHandler.OnError( exception );
                }

                SettingsHandler.OnError( e );
            }
        }

        #region IDisposable

        ~Item() { this.Icon?.Dispose(); }

        #endregion


        // ReSharper disable UnusedAutoPropertyAccessor.Global

        public Bitmap   Icon           { get; set; }
        public string   Name           { get; set; }
        public string   Path           { get; set; }
        public string   Size           { get; set; }
        public FileType Type           { get; private set; }
        public string   Extension      { get; }
        public bool     Exists         { get; }
        public bool     IsReadOnly     { get; }
        public DateTime CreationTime   { get; }
        public DateTime LastAccessTime { get; }
        public DateTime LastWriteTime  { get; }
        public double   SizePb         { get; set; }

        public DirectoryInfo TryGetDirectoryInfo { get; }

        public FileInfo TryGetFileInfo { get; }

        // ReSharper restore UnusedAutoPropertyAccessor.Global
    }
}
