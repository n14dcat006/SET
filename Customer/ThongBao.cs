using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Message;

namespace Customer
{
    public partial class ThongBao : Form
    {
        int maDH, maKH;
        long soTien;
        public ThongBao(string message, int maDonHang, int maKhachHang, long tien)
        {
            maDH = maDonHang;
            maKH = maKhachHang;
            soTien = tien;
            
            InitializeComponent();
            richTextBox1.Text = "";
            richTextBox1.AppendText(message);
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Common c = new Common();
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(iep);
            string message = "HUYDONHANG-" + maDH + "-" + maKH + "-" + soTien;
            c.send(message, ref client);
            client.Close();
            KetQua form = new KetQua("Khách hàng từ chối thanh toán, đơn hàng sẽ được hủy.");
            form.Show();
            this.Hide();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ThanhToan form = new ThanhToan(maDH, maKH, soTien);
            form.Show();
            this.Hide();
            this.Close();
        }
    }
}
