using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using Message;

namespace WebsiteThanhToan.Controllers
{
    public class HomeController : Controller
    {
        static string tenKH;
        static int soLuong;
        static string cardNumber, CVV, dateValid;
        static int maDH, maKH;
        static long soTien;
        static string customerPrivateKey = System.IO.File.ReadAllText("d:/file/customerPrivateKey.xml");
        static string merchantPublicKey, gatewayPublicKey;
        static X509Certificate2 customerCertificate = new X509Certificate2("d:/file/customer.crt");
        static X509Certificate2 merchantCertificate, gatewayCertificate;
        static X509Certificate2 caCertificate = new X509Certificate2("d:/file/ca.crt");
        public ActionResult Index(string oi)
        {
            if (oi == null)
            {
                oi = "1-bao baongoc-1-1-20000";
            }
            string[] split;
            split= oi.Split('-');
            maKH = Convert.ToInt32(split[0]);
            tenKH = split[1];
            maDH = Convert.ToInt32(split[2]);
            soLuong = Convert.ToInt32(split[3]);
            soTien = Convert.ToInt64(split[4]);

            ViewBag.TaiKhoan = tenKH;
            ViewBag.SoLuong = soLuong;
            ViewBag.SoTien = soTien;
            return View();
        }
        [HttpPost]
        public ActionResult Index(FormCollection collection)
        {
            bool verify;
            cardNumber = collection["CardNumber"];
            CVV = collection["CVV"];
            dateValid = collection["DateValid"];
            Common c = new Common();
            //lấy dữ liệu
            /*            
            System.IO.Stream s=PrivateKey.InputStream;
            byte[] buffer = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                byte[] privateKeyByte = ms.ToArray();
                customerPrivateKey = System.Text.Encoding.ASCII.GetString(privateKeyByte);
            }
            s.Flush();
            s = Certificate.InputStream;
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                byte[] certificateByte = ms.ToArray();
                customerCertificate = new X509Certificate2(certificateByte);
            }*/
            //khởi tạo kết nối đến customer
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
            if (c.VerifyCertificate(caCertificate, merchantCertificate) == false || c.VerifyCertificate(caCertificate, gatewayCertificate) == false)//xác thực chứng chỉ nhận được
            {
                ViewBag.thongbao = "Xác thực kết nối thất bại";
                ViewBag.TaiKhoan = tenKH;
                ViewBag.SoLuong = soLuong;
                ViewBag.SoTien = soTien;
                return this.View();
            }
            else
            {
                merchantPublicKey = merchantCertificate.GetRSAPublicKey().ToXmlString(false);
                gatewayPublicKey = gatewayCertificate.GetRSAPublicKey().ToXmlString(false);
                verify = c.Verify(merchantPublicKey, initRES[1], initRES[0]);
                if (verify == false)
                {
                    ViewBag.thongbao = "Xác thực kết nối thất bại";
                    ViewBag.TaiKhoan = tenKH;
                    ViewBag.SoLuong = soLuong;
                    ViewBag.SoTien = soTien;
                    return this.View();
                }
                else
                {
                    //tạo purchase request
                    string[] initREQValue = initRES[0].Split(':');
                    InitiateResponse initiateResponse = new InitiateResponse(initREQValue[0], initREQValue[1], initREQValue[2]);
                    OrderInfomation oi = new OrderInfomation(maDH, maKH, DateTime.Now.ToString("ddMMyyyy"), initiateResponse.getTransID(), initiateResponse.getBrandID(), soTien);
                    PaymentInstructions pi = new PaymentInstructions(cardNumber, CVV, dateValid, soTien, initiateResponse.getTransID(), initiateResponse.getBrandID());
                    PurchaseRequest purchaseRequest = new PurchaseRequest(oi.OIToString(), pi.PIToString(), customerPrivateKey, gatewayPublicKey, c.ByteArrayToString(customerCertificate.GetRawCertData()));
                    c.send(purchaseRequest.ToMessage(), ref client);
                    //nhận purchase response
                    receiveMessage = c.receive(ref client);
                    string[] splitRES = receiveMessage.Split('-');
                    PurchaseResponse purchaseResponse = new PurchaseResponse(splitRES[0], splitRES[1], splitRES[2]);
                    merchantCertificate = new X509Certificate2(c.StringToByteArray(purchaseResponse.getCertificate()));
                    if (c.VerifyCertificate(caCertificate, merchantCertificate) == false)//xác thực chứng chỉ từ purchase response
                    {
                        ViewBag.thongbao = "Xác thực kết nối thất bại";
                        ViewBag.TaiKhoan = tenKH;
                        ViewBag.SoLuong = soLuong;
                        ViewBag.SoTien = soTien;
                        return this.View();
                    }
                    else
                    {
                        if (purchaseResponse.verify() == false)
                        {
                            ViewBag.thongbao = "Xác thực kết nối thất bại";
                            ViewBag.TaiKhoan = tenKH;
                            ViewBag.SoLuong = soLuong;
                            ViewBag.SoTien = soTien;
                            return this.View();
                        }
                        else
                        {
                            string[] splitPurchase = purchaseResponse.getMessage().Split(':');//message = transid:RRPID:maKQ:KQ
                            if (splitPurchase[2].CompareTo("1") == 0)
                            {
                                return RedirectToAction("KetQua", new { kq = "Thanh toán thành công" });
                            }
                            else if (splitPurchase[2].CompareTo("2") == 0)
                            {
                                ViewBag.thongbao = "Thông tin tài khoản không đúng";
                                ViewBag.TaiKhoan = tenKH;
                                ViewBag.SoLuong = soLuong;
                                ViewBag.SoTien = soTien;
                                return this.View();
                            }
                            else if (splitPurchase[2].CompareTo("3") == 0)
                            {
                                ViewBag.thongbao = "Số tiền thanh toán vượt quá hạn mức của thẻ";
                                ViewBag.TaiKhoan = tenKH;
                                ViewBag.SoLuong = soLuong;
                                ViewBag.SoTien = soTien;
                                return this.View();
                            }
                            else
                            {
                                ViewBag.thongbao = "Xác thực kết nối thất bại";
                                ViewBag.TaiKhoan = tenKH;
                                ViewBag.SoLuong = soLuong;
                                ViewBag.SoTien = soTien;
                                return this.View();
                            }
                        }
                    }
                }
            }
        }
        public ActionResult KetQua(string kq)
        {
            ViewBag.thongbao = kq;
            return View();
        }


        public ActionResult About(string message)
        {
            ViewBag.Message =message;

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}