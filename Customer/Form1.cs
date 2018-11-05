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
using System.Windows.Forms;
using Message;

namespace Customer
{
    public partial class Form1 : Form
    {
        int maKH=1, maDH=1;
        long soTien=1;
        public Form1(string s)
        {
            //message = maKH - tenKH - maDH - soLuong - soTien 
            
            InitializeComponent();
            if (s.Length > 0)
            {
                string tenKH;
                int soLuong = 0;
                string[] splitMessage = s.Split('-');
                maKH = Convert.ToInt32(splitMessage[0]);
                tenKH = splitMessage[1];
                maDH = Convert.ToInt32(splitMessage[2]);
                soLuong = Convert.ToInt32(splitMessage[3]);
                soTien = Convert.ToInt64(splitMessage[4]);

                richTextBox1.AppendText("Khách hàng: " + tenKH + "\nSố lượng sản phẩm: " + soLuong.ToString() + "\nSố tiền thanh toán: " + soTien.ToString());

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Common c = new Common();
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(iep);
            string message = "HUYDONHANG-" + maDH + "-" + maKH+"-"+soTien;
            c.send(message,ref client);
            KetQua form = new KetQua("Khách hàng từ chối thanh toán, đơn hàng sẽ được hủy.");
            form.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ThanhToan form = new ThanhToan(maDH, maKH, soTien);
            form.Show();
            this.Hide();
        }
    }
}
