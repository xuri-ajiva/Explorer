using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Explorer {
    public partial class UserInterface : Form {
        public UserInterface(IHandler remoteHandler) {
            InitializeComponent();
            this.explorer1.Init( remoteHandler );
            this.explorer2.Init( new LocalHandler( "C:\\" ) );
        }

        private void UserInterface_FormClosed(object sender, FormClosedEventArgs e) { Environment.Exit( 0 ); }
    }
}