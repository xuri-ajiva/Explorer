//#define PerformanceTest

#region using

// ReSharper disable once RedundantUsingDirective
using ExplorerWpf.Handler;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExplorerWpf.CustomControls;
using ExplorerWpf.Pages;
using Brush = System.Windows.Media.Brush;
using MessageBox = System.Windows.Forms.MessageBox;

#endregion


namespace ExplorerWpf {
    public partial class MainWindow : Window, IDisposable {
        private ExplorerView _currentExplorerView;
        private IPage        _currentPage = new EmptyPage();

        private double _drag;
        private int    _linesToSkip;

        private TreePathItem        _root;
        private RowDefinition       _explorerNavigationBarRow;
        private SelectFolderTextBox _pathBar;

        public MainWindow() {
            InitializeComponent();
            this.CopyRightTextBox.Text = Program.Version + "    " + Program.CopyRight;
        }


        private IHandler Handler => this._currentExplorerView.Handler;

        private void StartConsole() {
            this._mainProcess = new Process {
                StartInfo = new ProcessStartInfo( SettingsHandler.UserPowerShell ? "C:\\windows\\system32\\windowspowershell\\v1.0\\powershell.exe" : "cmd.exe" ) {
                    RedirectStandardInput  = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    UseShellExecute        = false
                }
            };
            this._mainProcess.Start();
            this._mainProcess.StandardInput.AutoFlush = true;

            //p.BeginErrorReadLine();
            //p.BeginOutputReadLine();
            this._errReaderThread = new Thread( ConsoleError );
            this._outReaderThread = new Thread( ConsoleRead );
            this._inWriteThread   = new Thread( ConsoleWrite );
            this._errReaderThread.Start();
            this._outReaderThread.Start();
            this._inWriteThread.Start();

            this.ConsoleW.Init();
            this.ConsoleHost.MouseDown += this.ConsoleW.MouseDownFocusWindow;
        }

        private void GetControls() {
            var pathNode   = this.TabControl.Template.FindName( "PathBarX",             this.TabControl );
            var reloadNode = this.TabControl.Template.FindName( "ReloadButtonX",        this.TabControl );
            var rootNode   = this.TabControl.Template.FindName( "RootButtonX",          this.TabControl );
            var naviCNode  = this.TabControl.Template.FindName( "NavigationBarColumnX", this.TabControl );

            if ( pathNode is SelectFolderTextBox box ) {
                box.KeyDown  += BoxOnKeyDown;
                this._pathBar =  box;
                //Console.WriteLine( "PathBar Support" );
            }

            if ( reloadNode is Button reload ) reload.Click += ReloadOnClick;
            //Console.WriteLine( "Reload Support" );

            if ( rootNode is Button root ) root.Click += RootOnClick;
            //Console.WriteLine( "Disk List Support" );

            if ( naviCNode is RowDefinition row ) this._explorerNavigationBarRow = row;
            //Console.WriteLine( "Path bar hide support" );
        }

        private void WriteCmd(string command, bool echo = true) {
            Debug.WriteLine( echo + ": " + command );
            if ( this._mainProcess == null || this._mainProcess.HasExited ) return;

            this._mainProcess.StandardInput.WriteLine( command );
            if ( echo && SettingsHandler.ConsoleAutoChangePath )
                this._mainProcess.StandardInput.WriteLine( SettingsHandler.UserPowerShell ? "pwd" : "echo %cd%" );
        }

        #region ConsoleThreads

        private Thread  _errReaderThread;
        private Thread  _inWriteThread;
        private Thread  _outReaderThread;
        private Process _mainProcess;


        private void ConsoleRead() {
            const int negativeBegin = -100;

            this._mainProcess.StandardInput.WriteLine( SettingsHandler.UserPowerShell ? "function prompt {}" : "@echo off" );
            this._mainProcess.StandardInput.WriteLine( "cd /" );
            this._linesToSkip       = negativeBegin;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine( "Console Support Online" );
            Console.ForegroundColor = ConsoleColor.White; //TODO: Setting

            while ( !this._mainProcess.StandardOutput.EndOfStream && Program.Running ) {
                var line = this._mainProcess.StandardOutput.ReadLine();

                if ( this._linesToSkip <= negativeBegin ) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write( line );
                    Console.ForegroundColor = ConsoleColor.White;
                    this._linesToSkip       = SettingsHandler.UserPowerShell ? 6 : 4;
                    continue;
                }

                try {
                    var parches = Regex.Match( line, SettingsHandler.UserPowerShell ? "^[A-Za-z]:\\\\[^\r\n\"]*" : "[^\"^ ^\t]?[A-Za-z]:\\\\[^\"]*" );
                    Debug.WriteLine( line );

                    if ( parches.Success ) {
                        var path = parches.Value.Substring( 0, parches.Length );

                        while ( path.EndsWith( " " ) ) path = path.Substring( 0, path.Length - 1 );

                        if ( Directory.Exists( path ) )
                            if ( this.Handler.GetCurrentPath() != path ) {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.Write( path + "> " );
                                Console.ForegroundColor = ConsoleColor.White;

                                if ( SettingsHandler.ConsoleAutoChangePath ) {
                                    this.Handler.SetCurrentPath( path );
                                    this._currentExplorerView.ListP( this.Handler.GetCurrentPath(), true );
                                }

                                if ( !SettingsHandler.UserPowerShell )
                                    this._linesToSkip++;
                            }
                    }
                    else if ( SettingsHandler.UserPowerShell ) {
                        if ( this._linesToSkip <= 0 )
                            if ( !string.IsNullOrEmpty( line ) && !line.StartsWith( "Path" ) && !line.StartsWith( "----" ) && line != "PS>pwd" ) //Console.ForegroundColor = ConsoleColor.w;
                                Console.WriteLine( line );
                        //Console.ForegroundColor = ConsoleColor.White;
                    }
                } catch { }

                if ( !SettingsHandler.UserPowerShell )
                    if ( this._linesToSkip <= 0 )
                        if ( line != "echo %cd%" )
                            Console.WriteLine( line );

                if ( this._linesToSkip > 0 ) this._linesToSkip--;
            }
        }

        private void ConsoleWrite() {
            while ( Program.Running ) {
                var line = Console.In.ReadLine();
                this._linesToSkip++;
                WriteCmd( line );
            }
        }

        private void ConsoleError() {
            while ( !this._mainProcess.StandardError.EndOfStream && Program.Running ) {
                var line = this._mainProcess.StandardError.ReadLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( line );
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
        }

        #endregion


        #region WindowEvents

        private void DcChange(object sender, DependencyPropertyChangedEventArgs e) { Console.WriteLine( e ); }

        private void taps_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if ( this.TabControl.Items.Count == 1 ) return;

            if ( !( this.TabControl.SelectedItem is TabItem tp ) ) return;

            if ( tp.Content != null ) {
                if ( !( tp.Content is IPage page ) ) return;

                if ( page.Equals( this._currentPage ) ) return;

                this._currentPage = page;
                page.OnReFocus();

                if ( !page.ShowTreeView ) {
                    if ( this.TreeControl.Visibility == Visibility.Visible ) {
                        this.TreeControl.Visibility  = Visibility.Hidden;
                        this._drag                   = this.TreeColumn.Width.Value;
                        this.TreeColumn.Width        = new GridLength( 0 );
                        this.VerticalSplitter1.Width = new GridLength( 0 );
                    }
                }
                else if ( this.TreeControl.Visibility == Visibility.Hidden ) {
                    this.TreeControl.Visibility  = Visibility.Visible;
                    this.TreeColumn.Width        = new GridLength( this._drag );
                    this.VerticalSplitter1.Width = new GridLength( 6 );
                }

                this.Navigation.Visibility = page.HideNavigation ? Visibility.Hidden : Visibility.Visible;

                this.ConsoleHost.Visibility = page.HideConsole ? Visibility.Hidden : Visibility.Visible;

                this._explorerNavigationBarRow.Height = page.HideExplorerNavigation ? new GridLength( 0 ) : GridLength.Auto;

                switch (page) {
                    case EmptyPage ep:
                        //Console.WriteLine();
                        break;
                    case ExplorerView explorer:
                        this._currentExplorerView = explorer;
                        break;
                    case SettingsView sp:
                        //Console.WriteLine();
                        break;
                    case ThemeView ep:
                        //Console.WriteLine();
                        break;
                }
            }
            else {
                if ( this.TabControl.Items.Count > 1 ) this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
            }
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e) {
            if ( e.LeftButton != MouseButtonState.Pressed ) return;

            if ( this.WindowState == WindowState.Maximized ) this.WindowState = WindowState.Normal;

            try {
                DragMove();
            } catch (Exception exception) {
                HandlerOnOnError( exception );
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            NativeMethods.EnableBlur( new WindowInteropHelper( this ).Handle );
            GetControls();

            if ( SettingsHandler.ConsolePresent ) {
                StartConsole();
            }
            else {
                var consoleWrapper = new Thread( StartConsole );
                consoleWrapper.Start();
            }

            this._root      = TreePathItem.Empty;
            this._root.Icon = DefaultIcons.ShieldIcon;
            this._root.Name = SettingsHandler.ROOT_FOLDER;
            this._root.Path = SettingsHandler.ROOT_FOLDER;
            this.TreeControl.Items.Add( this._root );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.System ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.Desktop ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyMusic ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyPictures ) ) ) );
            this._root.Items.Add( new TreePathItem( new DirectoryInfo( Environment.GetFolderPath( Environment.SpecialFolder.MyVideos ) ) ) );

            foreach ( var driveInfo in DriveInfo.GetDrives() ) {
                var node = new TreePathItem( driveInfo.RootDirectory );
                this.TreeControl.Items.Add( node );
            }

        #if PerformanceTest
            CreateContextMenu();
            var t = new Thread( () => {
                const int MaxTaps = 100;

                var r = new Random();

                while ( true && Program.Running) {
                    Thread.Sleep( 2000 );

                    for ( int i = 0; i < MaxTaps; i++ ) {
                        this.Dispatcher?.Invoke( () => { AddTab( r.NextDouble() > .9 ? TapType.Settings : TapType.Explorer, "S:\\" ); } );

                        Thread.Sleep( 10 );
                    }

                    Thread.Sleep( 1000 );

                    for ( int i = 0; i < MaxTaps; i++ ) {
                        var fg = i;
                        this.Dispatcher?.Invoke( () => { this.TabControl.SelectedIndex = fg; } );
                        Thread.Sleep( 20 );
                    }

                    Thread.Sleep( 1000 );

                    for ( int i = 0; i < MaxTaps; i++ ) {
                        this.Dispatcher?.Invoke( () => { CloseTap( (TabItem) this.TabControl.Items[0] ); } );
                        Thread.Sleep( 20 );
                    }

                    Thread.Sleep( 1000 );

                    GC.Collect( 1, GCCollectionMode.Forced, true );
                }
            } );

            t.SetApartmentState( ApartmentState.STA );
            t.Start();
        #endif

            AddTab( TapType.EXPLORER );
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            var me = (MenuItem) sender;

            switch (( (MenuItem) sender ).TabIndex) {
                case 1:
                    AddTab( TapType.EXPLORER );
                    break;
                case 2:
                    AddTab( TapType.SETTINGS );
                    break;
                case 3:
                    AddTab( TapType.THEME );
                    break;
                case 99:
                    if ( me.IsChecked )
                        MessageBox.Show( "Test" );
                    break;

                case 100:
                    if ( ( (ContextMenu) me.Parent ).PlacementTarget is Control parent )
                        if ( parent.Parent is TabItem tp && tp.Content != null )
                            CloseTap( tp );

                    break;
            }
        }

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { AddTab( TapType.EXPLORER ); }

        private void MainWindow_OnClosed(object sender, EventArgs e) {
            try {
                foreach ( var item in this.TabControl.Items.Cast<TabItem>().Select( x => (IPage) x.Content ) ) item?.Dispose();
            } catch (Exception ex) {
                Console.WriteLine( ex.Message );
            }

            WriteCmd( "exit" );
            this.ConsoleW?.Dispose();
            this._mainProcess?.Close();
            this._outReaderThread?.Abort();
            this._inWriteThread?.Abort();
            Close();
            Dispose();
            //Environment.Exit( 0 );
        }

        // ReSharper disable once UnusedMember.Local
        private void DragMoveX(object sender, MouseButtonEventArgs e) { MoveWindow( sender, e ); }

        #endregion

        #region EventListener

        //Explorer Navigation Bar Events
        private void RootOnClick(object sender, RoutedEventArgs e) { this._currentExplorerView?.ListP( SettingsHandler.ROOT_FOLDER ); }

        private async void ReloadOnClick(object sender, RoutedEventArgs e) {
            this._currentExplorerView.MainView.Items.Clear();
            await Task.Delay( 10 );
            this._currentExplorerView?.ListP( this.Handler.GetCurrentPath(), true );
        }

        private void BoxOnKeyDown(object sender, KeyEventArgs e) {
            if ( e.Key != Key.Enter || this.Handler == null ) return;

            this.Handler.SetCurrentPath( ( sender as TextBox )?.Text );
            this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
        }

        //Handler Events  
        private void HOnOnSetCurrentPath(string arg1, string arg2) {
            if ( SettingsHandler.ConsoleAutoChangeDisc )
                if ( arg1.Length > 1 && arg2.Length > 1 ) {
                    if ( !string.Equals( arg1.Substring( 0, 2 ), arg2.Substring( 0, 2 ), StringComparison.CurrentCultureIgnoreCase ) ) {
                        WriteCmd( arg2.Substring( 0, 2 ), false );
                    }
                    else if ( SettingsHandler.ConsoleAutoChangePath && arg2.Length <= 3 && arg1 != arg2 ) {
                        var parches = Regex.Match( arg2, SettingsHandler.UserPowerShell ? "[A-Za-z]:\\\\?" : "[^\"^ ^\t]?[A-Za-z]:\\\\?" );

                        if ( parches.Success ) {
                            WriteCmd( "cd /", false );
                            var path = parches.Value.Substring( 0, 2 );
                            Console.WriteLine( "cd " + path + "\\" );
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write( path + "> " );
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                }

            if ( this._currentExplorerView == null ) return;
        }

        private static void HandlerOnOnError(Exception obj) { SettingsHandler.OnError( obj ); }

        //ExplorerView Events
        private void XOnSendDirectoryUpdateAsCmd(object sender, string e, bool cd) {
            if ( sender.Equals( this._currentExplorerView ) )
                WriteCmd( e, cd );
        }

        private void XOnUpdatePathBarDirect(object sender, string path) {
            if ( !sender.Equals( this._currentExplorerView ) ) return;

            this._pathBar.Text = path;
            if ( this._pathBar.Popup != null )
                this._pathBar.Popup.IsOpen = false;
        }

        private void XOnUpdateStatusBar(object arg1, string arg2, Brush arg3) {
            this.Dispatcher?.Invoke( () => {
                this.StatusBar.Foreground = arg3;
                this.StatusBar.Text       = arg2;
            } );
        }

        #endregion

        #region Buttons

        private void CloseClick(object sender, RoutedEventArgs e) { Close(); }

        private void MaxClick(object sender, RoutedEventArgs e) { this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal; }

        private void MinClick(object sender, RoutedEventArgs e) {
            //if ( WindowState == WindowState.Normal ) 
            this.WindowState = WindowState.Minimized;
        }

        private void PingClick(object sender, RoutedEventArgs e) { this.Topmost = !this.Topmost; }

        private void UpClick(object sender, RoutedEventArgs e) {
            var dir = this._currentExplorerView.GetDirUp();
            this.Handler.SetCurrentPath( dir );
            this._currentExplorerView.ListP( dir );
        }

        private void RootClick(object sender, RoutedEventArgs e) {
            this.Handler.SetCurrentPath( SettingsHandler.ROOT_FOLDER );
            this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
        }


        private void ForClick(object sender, RoutedEventArgs e) {
            if ( !this.Handler.HistoryHasFor ) return;

            this.Handler.GoInHistoryTo( this.Handler.HistoryIndex + 1 );
            this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
            HOnOnSetCurrentPath( "", "" );
        }

        private void BackClick(object sender, RoutedEventArgs e) {
            if ( !this.Handler.HistoryHasBack ) return;

            this.Handler.GoInHistoryTo( this.Handler.HistoryIndex - 1 );
            this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
            HOnOnSetCurrentPath( "", "" );
        }

        #endregion

        #region TreeControal

        private void trvMenu_MouseDown(object sender, MouseButtonEventArgs e) { }

        private void trvMenu_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if ( this.TreeControl.SelectedItem == null ) return;

            try {
                this.Handler.SetCurrentPath( ( (TreePathItem) this.TreeControl.SelectedItem ).Path );
                this._currentExplorerView.ListP( this.Handler.GetCurrentPath() );
            } catch {
                // ignored
            }
        }

        private void trvMenu_Expanded(object sender, RoutedEventArgs e) {
            if ( !( e.OriginalSource is TreeViewItem tvi ) ) return;
            if ( !( tvi.DataContext is TreePathItem node ) ) return;
            if ( node.Type == Item.FileType.NONE ) return;

            try {
                node.Items.Clear();
                this.Handler.SetCurrentPath( node.Path + "\\" );

                foreach ( var t in this.Handler.ListDirectory( node.Path ) ) {
                    TreePathItem n1;

                    try {
                        n1 = new TreePathItem( t );

                        try {
                            if ( this.Handler.ListDirectory( this.Handler.GetCurrentPath() ).Length == 0 )
                                n1.Items.Clear(); //clear default item
                        } catch (Exception exception) {
                            var tcs = TreePathItem.Empty;
                            tcs.Name = exception.Message;

                            n1.Items.Add( tcs );
                        }
                    } catch {
                        n1      = TreePathItem.Empty;
                        n1.Name = t.Name;
                    }

                    node.Items.Add( n1 );
                }
            } catch {
                // ignored
            }
        }

        private void trvMenu_Collapsed(object sender, RoutedEventArgs e) { }

        #endregion

        #region TabS

        private enum TapType {
            EXPLORER,
            SETTINGS,
            EMPTY,
            THEME
        }

        private ContextMenu _contextMenu;

        private void CreateContextMenu() {
            if ( this._contextMenu == null ) {
                var m = new ContextMenu();

                var me1 = new MenuItem { TabIndex = 100, Header = "_Close" };
                me1.Click += MenuItem_OnClick;

                m.Items.Add( me1 );
                this._contextMenu = m;
            }
        }

        private static EmptyPage CreateEmpty() => new EmptyPage();

        private static SettingsView CreateSettings() => new SettingsView();

        private static ThemeView CreateTheme() => new ThemeView();

        private ExplorerView CreateExplorer(string path = SettingsHandler.ROOT_FOLDER) {
            var h = new LocalHandler( path );
            h.OnError          += HandlerOnOnError;
            h.OnSetCurrentPath += HOnOnSetCurrentPath;

            var x = new ExplorerView();

            x.Init( h );

            x.SendDirectoryUpdateAsCmd += XOnSendDirectoryUpdateAsCmd;
            x.UpdateStatusBar          += XOnUpdateStatusBar;
            x.UpdatePathBarDirect      += XOnUpdatePathBarDirect;

            x.Margin = new Thickness( 0, 0, 0, 0 );
            return x;
        }


        private TabItem CreateTabItem(IPage page, string name) {
            var ex = (Label) this.PlusTabItem.Header;
            var l  = new Label { Content = name, Style = ex.Style, Margin = ex.Margin, FontSize = ex.FontSize, Effect = ex.Effect, Foreground = ex.Foreground, Background = ex.Background, BorderBrush = ex.BorderBrush, BorderThickness = ex.BorderThickness, ContextMenu = this._contextMenu };

            var newTabItem = new TabItem {
                Header  = l,
                Name    = name,
                Content = (UserControl) page,
                //Background  = this.ColorExample.Background,
                //Foreground  = this.ColorExample.Foreground,
                //BorderBrush = this.ColorExample.BorderBrush,
                Style = this.PlusTabItem.Style
            };
            page.ParentTapItem = newTabItem;
            return newTabItem;
        }

        private void AddTabToTabControl(TabItem newTabItem) {
            this.TabControl.Items.Add( new TabItem { Header = "Temp" } );
            var p = this.TabControl.Items[this.TabControl.Items.Count - 2];
            this.TabControl.Items[this.TabControl.Items.Count - 2] = newTabItem;
            this.TabControl.Items[this.TabControl.Items.Count - 1] = p;

            this.TabControl.SelectedIndex = this.TabControl.Items.Count - 2;
            taps_SelectionChanged( this, null );
        }

        private void AddTab(TapType type, string path = SettingsHandler.ROOT_FOLDER) {
            if ( this._contextMenu == null ) CreateContextMenu();

            IPage page;

            switch (type) {
                case TapType.EXPLORER:
                    page = CreateExplorer( path );
                    break;
                case TapType.SETTINGS:
                    page = CreateSettings();
                    break;
                case TapType.EMPTY:
                    page = CreateEmpty();
                    break;
                case TapType.THEME:
                    page = CreateTheme();
                    break;
                default: throw new ArgumentOutOfRangeException( nameof(type), type, null );
            }

            AddTabToTabControl( CreateTabItem( page, type.ToString() ) );
        }

        private void CloseTap(TabItem tp) {
            if ( !( tp.Content is IPage page ) ) return;

            page.Dispose();
            tp.Content = null;
            this.TabControl.Items.Remove( tp );
            GC.Collect( 0, GCCollectionMode.Forced );
            if ( this.TabControl.Items.Count == 1 ) CloseClick( null, null );
        }

        #endregion

        #region IDisposable

        private void Dispose(bool disposing) {
            if ( disposing ) {
                this.ConsoleW?.Dispose();
                this._currentExplorerView?.Dispose();
                this._currentPage?.Dispose();
                this._mainProcess.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        #endregion

    }
}
