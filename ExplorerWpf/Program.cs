#region using

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

#endregion

namespace ExplorerWpf {
    internal class Program {
        public static readonly string CopyRight = FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly()?.Location ).LegalCopyright;
        public static readonly string Version   = FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly()?.Location ).ProductVersion;

        [STAThread]
        public static void Main(string[] args) {
            if ( !SettingsHandler.ConsolePresent )
                SettingsHandler.NativeMethods.ShowWindowAsync( SettingsHandler.NativeMethods.GetConsoleWindow(), SettingsHandler.NativeMethods.SW_HIDE );
            var app = new App();
            app.InitializeComponent();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            //Application.Current.Resources["Background"]   = new SolidColorBrush( Colors.DarkCyan );
            //Application.Current.Resources["DefBack"]      = new SolidColorBrush( Colors.DarkGreen );
            //Application.Current.Resources["Accent"]       = new SolidColorBrush( Colors.Chartreuse );
            //Application.Current.Resources["WindowBorder"] = new SolidColorBrush( Colors.DodgerBlue );
            //Application.Current.Resources["Border"] = new SolidColorBrush( Colors.Crimson );
            //Application.Current.Resources["ScrollBarBackground"] = new SolidColorBrush( Colors.DarkOrange );

            //Application.Current.Resources["Foreground"] = new LinearGradientBrush( Colors.Aqua, Colors.Magenta, new Point( 0, 0 ), new Point( 1, 1 ) );

            app.Run();
        }
    }
}
