#region using

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

#endregion

namespace ExplorerWpf {
    internal class Program {
        public static readonly string CopyRight = FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly()?.Location ).LegalCopyright;
        public static readonly string Version   = FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly()?.Location ).ProductVersion;
        public static          bool   Running { get; private set; }

        private static ConsoleColor ClosestConsoleColor(byte r, byte g, byte b) {
            ConsoleColor ret = 0;
            double       rr  = r, gg = g, bb = b, delta = double.MaxValue;

            foreach ( ConsoleColor cc in Enum.GetValues( typeof(ConsoleColor) ) ) {
                var n = Enum.GetName( typeof(ConsoleColor), cc );
                var c = Color.FromName( n == "DarkYellow" ? "Orange" : n ); // bug fix
                var t = Math.Pow( c.R - rr, 2.0 ) + Math.Pow( c.G - gg, 2.0 ) + Math.Pow( c.B - bb, 2.0 );
                if ( t == 0.0 )
                    return cc;

                if ( t < delta ) {
                    delta = t;
                    ret   = cc;
                }
            }

            return ret;
        }

        [STAThread]
        public static void Main(string[] args) {
            if ( !SettingsHandler.ConsolePresent )
                NativeMethods.ShowWindowAsync( NativeMethods.GetConsoleWindow(), NativeMethods.SW_HIDE );

            Running = true;
            var app = new App();
            app.InitializeComponent();

            var c = SettingsHandler.Color1.Border.ToColor();

            //var conColor = ClosestConsoleColor( c.R, c.G, c.B );  
            Console.BackgroundColor = ConsoleColor.Black;
            //Console.BackgroundColor = conColor;
            //Console.WriteLine( "Console Color: " + conColor );

            SettingsHandler.LoadCurrentColor();
            app.Run();
            Running = false;
            //Console.WriteLine( "Exit" );
            //Console.ReadLine();
        }
    }
}
