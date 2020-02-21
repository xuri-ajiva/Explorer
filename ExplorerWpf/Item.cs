using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using ExplorerWpf.Handler;

namespace ExplorerWpf {
    public static class DefaultIcons {

        private const uint SHGFI_ICON      = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;

        // ReSharper disable once UnusedMember.Local
        private const           uint                     SHGFI_SMALLICON = 0x000000001;
        private static readonly Dictionary<string, Icon> Cache           = new Dictionary<string, Icon>();

        public static Icon GetFileIconCashed(string path) {
            var ext = path;

            if ( File.Exists( path ) ) ext = Path.GetExtension( path );

            if ( ext == null )
                return null;

            Icon icon;
            if ( Cache.TryGetValue( ext, out icon ) )
                return icon;

            icon = ExtractFromPath( path );
            Cache.Add( ext, icon );
            return icon;
        }

        private static Icon ExtractFromPath(string path) {
            var shinfo = new SHFILEINFO();
            SHGetFileInfo( path, 0, ref shinfo, (uint) Marshal.SizeOf( shinfo ), SHGFI_ICON | SHGFI_LARGEICON );
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

        public TreePathItem(DirectoryInfo d) : base( d ) { Init(false); }
        public TreePathItem(FileInfo      f) : base( f ) { Init(false); }

        // ReSharper disable once RedundantBaseConstructorCall
        protected TreePathItem() : base() { Init(true); }

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
            this.Size           = f.Length + $" ({GetLenght( f.Length )})";
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
            this.Icon           = SystemIcons.Hand.ToBitmap();
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
                var i = new Item { Path = LocalHandler.ROOT_FOLDER, Name = LocalHandler.ROOT_FOLDER, Type = FileType.DIRECTORY, Size = long.MaxValue + $" ({GetLenght( long.MaxValue )})" };
                i.Icon.Dispose();
                i.Icon = SystemIcons.Shield.ToBitmap();
                return i;
            }
        }


        [DebuggerStepThrough]
        public static string GetLenght(long length) {
            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "0.00" ) + "Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "0.00" ) + "Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "0.00" )   + "Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "0.00" )   + "Mb";
            if ( length > Math.Pow( 10, 3 ) ) return ( length / Math.Pow( 10, 3 ) ).ToString( "0.00" )   + "Kb";

            return length + "b";
        }

        private void CreateIcon() {
            try {
                this.Icon = this.Path != LocalHandler.ROOT_FOLDER ? DefaultIcons.GetFileIconCashed( this.Path ).ToBitmap() : SystemIcons.Shield.ToBitmap();
            } catch (Exception e) {
                try {
                    this.Icon = SystemIcons.Error.ToBitmap();
                } catch (Exception exception) {
                    Console.WriteLine( exception );
                }

                Console.WriteLine( e.Message );
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
        public Double   SizePb         { get; set; }

        public DirectoryInfo TryGetDirectoryInfo { get; }

        public FileInfo TryGetFileInfo { get; }

        // ReSharper restore UnusedAutoPropertyAccessor.Global
    }
}
