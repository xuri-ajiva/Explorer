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
        private readonly IHandler _remoteHandler;

        public UserInterface(IHandler remoteHandler) {
            this._remoteHandler = remoteHandler;
            InitializeComponent();
        }

        private void UserInterface_FormClosed(object sender, FormClosedEventArgs e) { Environment.Exit( 0 ); }

        private void explorerClass1_Load(object sender, EventArgs e) { this.explorerClass1.Init( this._remoteHandler ); }

        private void explorerClass2_Load(object sender, EventArgs e) { this.explorerClass2.Init( new LocalHandler( "C:\\" ) ); }
    }
}