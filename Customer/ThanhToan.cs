using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using Message;
namespace Customer
{
    public partial class ThanhToan : Form
    {
        string cardNumber, CVV, dateValid;
        int maDH, maKH;
        long soTien;
        string customerPrivateKey;
        string merchantPublicKey, gatewayPublicKey;
        X509Certificate2 customerCertificate, merchantCertificate, gatewayCertificate;
        X509Certificate2 caCertificate = new X509Certificate2("d:/file/ca.crt");
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            cardNumber = textBox1.Text;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            CVV = textBox2.Text;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            dateValid = textBox3.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pathToFile="";
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Open Certificate File";
            theDialog.InitialDirectory = @"C:\";
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                pathToFile = theDialog.FileName;
                label6.Text = pathToFile;
            }
            if (File.Exists(pathToFile))
            {
                customerCertificate = new X509Certificate2(pathToFile);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string pathToFile = "";
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Open Private Key File";
            theDialog.InitialDirectory = @"C:\";
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                pathToFile = theDialog.FileName;
                label5.Text = pathToFile;
            }
            if (File.Exists(pathToFile))
            {
                customerPrivateKey = File.ReadAllText(pathToFile);
            }
        }

        
        public ThanhToan(int maDonHang, int maKhachHang, long tien)
        {
            maDH = maDonHang;
            maKH = maKhachHang;
            soTien = tien;
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool verify;
            string s;
            Common c = new Common();
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(iep);

            //tạo init request gửi tới merchant
            InitiateRequest initiateRequest = new InitiateRequest();
            c.send(initiateRequest.ToMessage(), ref client);
            //nhận init response từ merchant
            string receiveMessage = c.receive(ref client);
            string[] initRES = receiveMessage.Split('-');
            merchantCertificate = new X509Certificate2(c.StringToByteArray(initRES[2]));
            gatewayCertificate = new X509Certificate2(c.StringToByteArray(initRES[3]));
            if (c.VerifyCertificate(caCertificate,merchantCertificate) == false || c.VerifyCertificate(caCertificate,gatewayCertificate) == false)//xác thực chứng chỉ nhận được
            {
                s = "Xác thực kết nối thất bại";
                openThongBaoForm(s);
            }
            else
            {
                merchantPublicKey = merchantCertificate.GetRSAPublicKey().ToXmlString(false);
                gatewayPublicKey = gatewayCertificate.GetRSAPublicKey().ToXmlString(false);
                verify = c.Verify(merchantPublicKey, initRES[1], initRES[0]);
                if (verify == false)
                {
                    s = "Xác thực kết nối thất bại";
                    openThongBaoForm(s);
                }
                else
                {
                    //tạo purchase request
                    string[] initREQValue = initRES[0].Split(':');
                    InitiateResponse initiateResponse = new InitiateResponse(initREQValue[0], initREQValue[1], initREQValue[2]);
                    OrderInfomation oi = new OrderInfomation(maDH,maKH, DateTime.Now.ToString("ddMMyyyy"), initiateResponse.getTransID(), initiateResponse.getBrandID(),soTien);
                    PaymentInstructions pi = new PaymentInstructions(cardNumber,CVV,dateValid,soTien, initiateResponse.getTransID(), initiateResponse.getBrandID());                    
                    PurchaseRequest purchaseRequest = new PurchaseRequest(oi.OIToString(), pi.PIToString(), customerPrivateKey, gatewayPublicKey, c.ByteArrayToString(customerCertificate.GetRawCertData()));
                    c.send(purchaseRequest.ToMessage(), ref client);
                    //nhận purchase response
                    receiveMessage = c.receive(ref client);
                    string[] splitRES = receiveMessage.Split('-');
                    PurchaseResponse purchaseResponse = new PurchaseResponse(splitRES[0], splitRES[1], splitRES[2]);
                    merchantCertificate = new X509Certificate2(c.StringToByteArray(purchaseResponse.getCertificate()));
                    if (c.VerifyCertificate(caCertificate,merchantCertificate) == false)//xác thực chứng chỉ từ purchase response
                    {
                        s = "Xác thực kết nối thất bại";
                        openThongBaoForm(s);
                    }
                    else
                    {
                        if (purchaseResponse.verify() == false)
                        {
                            s = "Xác thực kết nối thất bại";
                            openThongBaoForm(s);
                        }
                        else
                        {
                            string[] splitPurchase = purchaseResponse.getMessage().Split(':');//message = transid:RRPID:maKQ:KQ
                            if (splitPurchase[2].CompareTo("1") == 0)
                            {
                                KetQua form = new KetQua("Thanh toán thành công");
                                form.Show();
                                this.Hide();
                                this.Close();
                            }
                            else if (splitPurchase[2].CompareTo("2") == 0)
                            {
                                s = "Thông tin tài khoản không đúng";
                                openThongBaoForm(s);
                            }
                            else
                            {
                                s = "Xác thực kết nối thất bại";
                                openThongBaoForm(s);
                            }
                        }
                    }
                    
                    client.Close();
                }
            }
            
        }
        void openThongBaoForm(string s)
        {
            
            ThongBao thongBao = new ThongBao(s, maDH, maKH, soTien);
            thongBao.Show();
            this.Hide();
            this.Close();
        }
    }
}
