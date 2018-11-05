using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Customer
{
    public partial class KetQua : Form
    {
        public KetQua(string s)
        {
            
            InitializeComponent();
            richTextBox1.AppendText(s);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
