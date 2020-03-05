#region using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ThreadState = System.Threading.ThreadState;

#endregion

namespace ExplorerWpf {
    public class ImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                if ( value is Bitmap bitmap1 ) {
                    var stream = new MemoryStream();
                    bitmap1.Save( stream, ImageFormat.Png );

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    return bitmap;
                }
            } catch (Exception e) {
                Debug.WriteLine( e );
                //SettingsHandler.OnError( e );
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public static class DefaultIcons {

        private const uint SHGFI_ICON      = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;

        // ReSharper disable once UnusedMember.Local
        private const uint SHGFI_SMALLICON = 0x000000001;

        private static Bitmap _errorCash;
        private static Icon   _errorCashI;
        private static Bitmap _shieldCash;
        private static Bitmap _warningCash;
        private static Bitmap _loadingCash;

        private static readonly Dictionary<string, Icon> Cache     = new Dictionary<string, Icon>();
        private static readonly object                   CacheLock = new object();

        private static readonly Queue<(string, string)> CashQueue = new Queue<(string, string)>();

        private static Thread _cashThread = new Thread( CashLoop );

        public static Bitmap ErrorIcon  => _errorCash  ?? ( _errorCash = ErrorIconI.ToBitmap() );
        public static Icon   ErrorIconI => _errorCashI ?? ( _errorCashI = SystemIcons.Error );

        public static Bitmap ShieldIcon {
            get {
                if ( _shieldCash                  == null ) _shieldCash                 = SystemIcons.Shield.ToBitmap();
                else if ( _shieldCash.PixelFormat == PixelFormat.DontCare ) _shieldCash = SystemIcons.Shield.ToBitmap();

                return _shieldCash;
            }
        }

        public static Bitmap WarningIcon => _warningCash ?? ( _warningCash = SystemIcons.Warning.ToBitmap() );

        public static Bitmap LoadingIcon => _loadingCash ?? ( _loadingCash = SystemIcons.Hand.ToBitmap() );

        public static Icon GetFileIconCashed(string path) {
            var ext = path;

            if ( File.Exists( path ) ) ext = Path.GetExtension( path );

            if ( SettingsHandler.ExtenstionWithSpecialIcons.Contains( ext ) ) ext = path;

            if ( ext == null ) return null;

            lock (CacheLock) {
                if ( Cache.TryGetValue( ext, out var icon ) ) return icon;

                icon = ExtractFromPath( path );

                if ( icon == null ) return null;

                Cache.Add( ext, icon );
                return icon;
            }
        }

        private static Icon ExtractFromPath(string path) {
            var shinfo = new SHFILEINFO();
            SHGetFileInfo( path, 0, ref shinfo, (uint) Marshal.SizeOf( shinfo ), SHGFI_ICON | SHGFI_LARGEICON );

            if ( shinfo.hIcon == IntPtr.Zero ) return default;

            return Icon.FromHandle( shinfo.hIcon );
        }

        [DllImport( "shell32.dll" )]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        public static void CacheIcon(string path) {
            string key;

            if ( File.Exists( path ) ) {
                var ext = Path.GetExtension( path );
                key = SettingsHandler.ExtenstionWithSpecialIcons.Contains( ext ) ? path : ext;
            }
            else {
                key = path;
            }

            if ( key == null ) return;

            CashQueue.Enqueue( ( key, path ) );

            if ( _cashThread != null && _cashThread.ThreadState == ThreadState.Unstarted ) _cashThread.Start();

            if ( _cashThread == null || !_cashThread.IsAlive ) {
                _cashThread = new Thread( CashLoop );
                _cashThread.Start();
            }
        }

        public static void CashLoop() {
            try {
                while ( Program.Running ) {
                    while ( CashQueue == null || CashQueue.Count == 0 ) {
                        if ( !Program.Running ) return;

                        Thread.Sleep( 20 );
                    }

                    ( var ext, var path ) = CashQueue.Dequeue();

                    lock (CacheLock) {
                        if ( Cache.ContainsKey( ext ) ) continue;

                        var icon = ExtractFromPath( path );

                        if ( icon == null ) continue;

                        Cache.Add( ext, icon );
                    }
                }
            } catch (Exception e) {
                Console.WriteLine( e.Message );
            }
        }

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
            if ( !noSubNode ) this.Items.Add( Empty );
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

            this.Icon = DefaultIcons.LoadingIcon;
            PreLoadIcon();
            //CreateIcon();
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

            this.Icon = DefaultIcons.LoadingIcon;
            PreLoadIcon();
            ApplyFixes();
            //CreateIcon();
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
                i.ApplyFixes();
                return i;
            }
        }


        [DebuggerStepThrough]
        public static string GetLength(long length) {
            if ( length > Math.Pow( 10, 15 ) ) return ( length / Math.Pow( 10, 15 ) ).ToString( "000.00" ) + " Pb";
            if ( length > Math.Pow( 10, 12 ) ) return ( length / Math.Pow( 10, 12 ) ).ToString( "000.00" ) + " Tb";
            if ( length > Math.Pow( 10, 9 ) ) return ( length / Math.Pow( 10, 9 ) ).ToString( "000.00" )   + " Gb";
            if ( length > Math.Pow( 10, 6 ) ) return ( length / Math.Pow( 10, 6 ) ).ToString( "000.00" )   + " Mb";
            if ( length > Math.Pow( 10, 3 ) ) return ( length / Math.Pow( 10, 3 ) ).ToString( "000.00" )   + " Kb";

            return length + " b";
        }

        private void PreLoadIcon() { DefaultIcons.CacheIcon( this.Path ); }

        public void ApplyFixes() {
            if ( this.Type == FileType.DIRECTORY )
                while ( this.Path.EndsWith( "\\" ) )
                    this.Path = this.Path.Substring( 0, this.Path.Length - 1 );
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

        private Bitmap _icon;

        public Bitmap Icon {
            get {
                if ( this._icon == DefaultIcons.LoadingIcon ) CreateIcon();

                return this._icon;
            }
            set => this._icon = value;
        }

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
