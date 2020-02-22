#region using

using System;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace ExplorerWpf {
    internal class Program {
        public static readonly string CopyRight = FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly()?.Location ).LegalCopyright;
        public static readonly string Version   = FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly()?.Location ).ProductVersion;

        [STAThread]
        public static void Main(string[] args) {
            if ( !SettingsHandler.ConsolePresent )
                SettingsHandler.DllImport.ShowWindowAsync( SettingsHandler.DllImport.GetConsoleWindow(), SettingsHandler.DllImport.SW_HIDE );
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
