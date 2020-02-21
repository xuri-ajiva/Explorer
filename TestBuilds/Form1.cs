using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestBuilds {
    public partial class Form1 : Form {


        public Form1() { InitializeComponent(); }

        private void Form1_Load(object sender, EventArgs e) {
            new Thread( () => {
                while ( this.Visible ) {
                    Thread.Sleep( new Random().Next( 10, 1000 ) );

                    Console.WriteLine( new Random().NextDouble() );
                    Console.WriteLine( "pleas input anything:" );
                    var imn = Console.ReadLine();
                    Console.WriteLine( imn );
                }
            } ).Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {Environment.Exit( 0 ); }
    }
}
