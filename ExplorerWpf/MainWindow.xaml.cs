using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ConsoleControlAPI;
using ExplorerBase;
using ExplorerBase.Handlers;
using ExplorerBase.UI;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        [DllImport( "kernel32" )]
        static extern bool AllocConsole();

        /*const int GripSize   = 16;
        const int BorderSize = 7;

        ConsoleContent dc = new ConsoleContent();*/

        public MainWindow() {
            //AllocConsole();
            InitializeComponent();
            /*DataContext =  dc;
            Loaded      += MainWindow_Loaded;*/
            //this.consoleX.ShowDiagnostics = true;   
            this.consoleX.InitializeComponent();
            this.consoleX.OnProcessOutput += ConsoleXOnOnProcessOutput;
            this.consoleX.OnProcessInput  += ConsoleXOnOnProcessInput;
            this.consoleX.StartProcess( "cmd.exe", "" );
            this.consoleX.IsInputEnabled = true;
            this.consoleX.Visibility     = Visibility.Collapsed;
            this.consoleX.Foreground = Brushes.LimeGreen;
            consoleX.FontStyle = new FontStyle(){};
        }


        private bool first = false;

        private void ConsoleXOnOnProcessInput(object sender, ProcessEventArgs args) { }


        TimeSpan last = TimeSpan.Zero;

        private void ConsoleXOnOnProcessOutput(object sender, ProcessEventArgs args) {
            if ( !this.first ) {
                new Thread( () => {
                    Thread.Sleep( 1000 );

                    var dispatcher = this.Dispatcher;

                    if ( dispatcher != null ) {
                        dispatcher.Invoke( () => { this.consoleX.ProcessInterface.WriteInput( "@echo off" ); } );
                        Thread.Sleep( 100 );
                        dispatcher.Invoke( () => {
                            this.consoleX.ClearOutput();
                            this.consoleX.WriteOutput( "Console Support Enabled!\n", Color.FromRgb( 0, 129, 255 ) );
                            this.consoleX.Visibility = Visibility.Visible;
                        } );
                    }
                    else { }
                } );//.Start();
                 
                this.consoleX.Visibility = Visibility.Visible;
                this.first = true;
            }

            if ( args.Code.HasValue ) {
                this.consoleX.WriteOutput( $"[{args.Code.Value}]", Colors.DarkBlue );
            }

            //if ( Regex.IsMatch( args.Content, @"[A-Z]:\\[^>]*>" ) ) {
            this.consoleX.WriteOutput( "> ", Colors.Yellow );
            this.consoleX.WriteOutput( " ", Colors.DeepSkyBlue );
            //}
        }

        /*void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            InputBlock.KeyDown += InputBlock_KeyDown;
            InputBlock.Focus();
        }

        void InputBlock_KeyDown(object sender, KeyEventArgs e) {
            if ( e.Key == Key.Enter ) {
                dc.ConsoleInput = InputBlock.Text;
                dc.RunCommand();
                InputBlock.Focus();
                Scroller.ScrollToBottom();
            }
        }*/

        /*protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr     windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource windowSource = HwndSource.FromHwnd(windowHandle);
            windowSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd,   int    msg,
            IntPtr                    wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_NCHITTEST)
            {
                int   x   = lParam.ToInt32() << 16 >> 16, y = lParam.ToInt32() >> 16;
                Point pos = PointFromScreen(new Point(x, y));

                if (pos.X > GripSize                 && 
                    pos.X < ActualWidth   - GripSize &&
                    pos.Y >= ActualHeight - BorderSize)
                {
                    return (IntPtr)NativeMethods.HTBOTTOM; // This doesn't work?
                }

                // Top, Left, Right, Corners, Etc.
            }

            return IntPtr.Zero;
        }*/
        private void CloseClick(object sender, RoutedEventArgs e) { this.Close(); }

        private void MaxClick(object sender, RoutedEventArgs e) { this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal; }

        private void MinClick(object sender, RoutedEventArgs e) {
            //if ( WindowState == WindowState.Normal ) 
            WindowState = WindowState.Minimized;
        }

        private void PingClick(object sender, RoutedEventArgs e) { this.Topmost = !Topmost; }

        private void MoveWindow(object sender, MouseButtonEventArgs e) { this.DragMove(); }

        private void listview_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        ExplorerClass             c;
        private LocalHandler le = new LocalHandler( "C:\\" );

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            System.Windows.Forms.Integration.WindowsFormsHost host =
                new System.Windows.Forms.Integration.WindowsFormsHost();

            c = new ExplorerClass();

        this.le.OnSetCurrentPath += Update;
        this.le.OnSetRemotePath += Update;

            this.c.Init(le);
            this.c.Dock = DockStyle.Fill;
            host.Child  = this.c;
            this.grid1.Children.Add( host );
        }

        private void Update() {
            this.consoleX.ProcessInterface.WriteInput( "cd \""+le.GetCurrentPath() + "\"" );
        }
    }

    /*public class ConsoleContent : INotifyPropertyChanged {
        string                       consoleInput  = string.Empty;
        ObservableCollection<string> consoleOutput = new ObservableCollection<string>() { "Console Emulation Sample..." };

        public string ConsoleInput {
            get { return consoleInput; }
            set {
                consoleInput = value;
                OnPropertyChanged( "ConsoleInput" );
            }
        }

        public ObservableCollection<string> ConsoleOutput {
            get { return consoleOutput; }
            set {
                consoleOutput = value;
                OnPropertyChanged( "ConsoleOutput" );
            }
        }

        public void RunCommand() {
            //ConsoleOutput.Add( ConsoleInput );

            if ( ConsoleInput.Length >= 4 && ConsoleInput.Substring( 0, 3 ) == "say" ) {
                ConsoleOutput.Add( ConsoleInput.Substring( string.IsNullOrWhiteSpace( ConsoleInput[3].ToString() ) ? 4 : 3 ) );
            }

            // do your stuff here.
            ConsoleInput = String.Empty;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            if ( null != PropertyChanged )
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }*/
    /*public class NativeMethods
    {
        public const int WM_NCHITTEST  = 0x84;
        public const int HTCAPTION     = 2;
        public const int HTLEFT        = 10;
        public const int HTRIGHT       = 11;
        public const int HTTOP         = 12;
        public const int HTTOPLEFT     = 13;
        public const int HTTOPRIGHT    = 14;
        public const int HTBOTTOM      = 15;
        public const int HTBOTTOMLEFT  = 16;
        public const int HTBOTTOMRIGHT = 17;
    }*/

}
