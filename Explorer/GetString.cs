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
    public partial class GetString : Form {
        public string outref = "";

        public GetString(String Info) {
            InitializeComponent();
        }

        private void Ok_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.OK;
            this.outref  = this.textBox1.Text;
            this.Close();
        }

        private void Cancle_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            this.outref = null;
            this.Close();
        }
    }
}