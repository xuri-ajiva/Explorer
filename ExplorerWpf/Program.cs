#region using

using System;

#endregion

namespace ExplorerWpf {
    internal class Program {
        [STAThread]
        public static void Main(string[] args) {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
