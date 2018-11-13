using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Message;

namespace Merchant
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 1234);
            listener.Start();
            while (true)
            {
                Socket socket = listener.AcceptSocket();
                Thread t = new Thread(new ParameterizedThreadStart(Thread1));
                t.Start(socket);
            }
        }
        public static void Thread1(object sock)
        {
            int maDH;
            Console.WriteLine("start connect with customer client");
            X509Certificate2 caCertificate = new X509Certificate2("d:/file/ca.crt");
            string gatewayPublicKey;
            string merchantPrivateKey = File.ReadAllText("d:/file/MerchantPrivateKey.xml");
            X509Certificate2 merchantCertificate = new X509Certificate2("d:/file/merchant.crt");
            X509Certificate2 gatewayCertificate = new X509Certificate2("d:/file/gateway.crt");
            X509Certificate2 customerCertificate;

            Common c = new Common();
            Socket socket = (Socket)sock;

            string receiveMessage = c.receive(ref socket);
            string[] firstMessage = receiveMessage.Split('-');
            string s;
            //thông điệp nhận được là Hủy đơn hàng hoặc init request
            if (firstMessage[0].CompareTo("HUYDONHANG") == 0)
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "localhost";
                builder.UserID = "sa";
                builder.Password = "123456";
                builder.InitialCatalog = "QuanLyBanSach";
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    string sql;
                    StringBuilder sb = new StringBuilder();
                    
                    sb.Append("DELETE FROM ChiTietDonHang WHERE MaDonHang = @maDH;");
                    sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@maDH", firstMessage[1]);
                        int rowsAffected = command.ExecuteNonQuery();
                    }
                    sb.Clear();
                    sb.Append("DELETE FROM DonHang WHERE MaDonHang = @maDH;");
                    sql = sb.ToString();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@maDH", firstMessage[1]);
                        int rowsAffected = command.ExecuteNonQuery();
                    }
                    
                    connection.Close();
                }
            }
            else
            {
                InitiateRequest initiateRequest = new InitiateRequest(firstMessage[0], firstMessage[1], firstMessage[2], firstMessage[3], firstMessage[4]);
                
                //tạo init response
                gatewayPublicKey = gatewayCertificate.GetRSAPublicKey().ToXmlString(false);
                InitiateResponse initiateResponse = new InitiateResponse(initiateRequest.getLIDC(), initiateRequest.getLanguage(), initiateRequest.getRRPID(), initiateRequest.getBrandID(), c.ByteArrayToString(merchantCertificate.GetRawCertData()), c.ByteArrayToString(gatewayCertificate.GetRawCertData()));
                string sendMessage = initiateResponse.ToMessage(merchantPrivateKey);
                c.send(sendMessage, ref socket);

                //nhận purchase request
                receiveMessage = c.receive(ref socket);
                string[] purchase = receiveMessage.Split('-');
                customerCertificate = new X509Certificate2(c.StringToByteArray(purchase[5]));
                if (c.VerifyCertificate(caCertificate,customerCertificate) == false)
                {
                    Console.WriteLine("verify purchase request certificate false");
                    s = initiateResponse.getTransID() + ":" + c.Random(2)+ ":" + 3 + ":" + "xac thuc that bai";
                    PurchaseResponse purchaseResponse = new PurchaseResponse(s);
                    c.send(purchaseResponse.ToMessage(), ref socket);
                }
                else
                {
                    PurchaseRequest purchaseRequest = new PurchaseRequest(purchase[0], purchase[1], purchase[2], purchase[3], purchase[4], purchase[5]);
                    if (purchaseRequest.verify() == false)//xác thực purchase request
                    {
                        Console.WriteLine("verify purchase request false");
                        s = initiateResponse.getTransID() + ":" + purchaseRequest.getRRPID() + ":" + 3 + ":" + "xac thuc that bai";
                        PurchaseResponse purchaseResponse = new PurchaseResponse(s);
                        c.send(purchaseResponse.ToMessage(), ref socket);
                    }
                    else
                    {
                        Console.WriteLine("verify purchase request true");
                        maDH = purchaseRequest.getMaDH();
                        //tạo authorization request gửi tới gateway
                        AuthorizationRequest authorizationRequest = new AuthorizationRequest(purchaseRequest.getTransID(), Convert.ToDouble(purchaseRequest.getTien()), merchantPrivateKey, gatewayPublicKey, purchaseRequest.getCustommerCertificate(), c.ByteArrayToString(merchantCertificate.GetRawCertData()), purchaseRequest.getMessageToGateway(), purchaseRequest.getDigitalEnvelop());

                        IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1235);
                        Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        client.Connect(iep);

                        c.send(authorizationRequest.ToMessage(), ref client);

                        //nhận authorization response
                        receiveMessage = c.receive(ref client);
                        string[] splitAuthRES = receiveMessage.Split('-');
                        AuthorizationResponse authorizationResponse;
                        if (splitAuthRES.Length < 5)//trường hợp nhận thông báo lỗi từ isuuer
                        {
                            gatewayCertificate = new X509Certificate2(c.StringToByteArray(splitAuthRES[3]));
                            if (c.VerifyCertificate(caCertificate, gatewayCertificate) == true)//kiểm tra chứng chỉ nhận từ gateway
                            {
                                Console.WriteLine("verify authorization response certificate true");
                                authorizationResponse = new AuthorizationResponse(splitAuthRES[0], splitAuthRES[1], splitAuthRES[2], splitAuthRES[3]);
                                if (authorizationResponse.verifyMessage() == true)
                                {
                                    Console.WriteLine("verify authorization response true");

                                    //tạo purchase response và gởi customer
                                    string[] messageRES = authorizationResponse.getMessage().Split(':');
                                    PurchaseResponse purchaseResponse = new PurchaseResponse(messageRES[0] + ":" + purchaseRequest.getRRPID() + ":" + messageRES[2] + ":" + messageRES[3]);
                                    c.send(purchaseResponse.ToMessage(), ref socket);
                                }
                            }
                           
                        }
                        else
                        {
                            gatewayCertificate = new X509Certificate2(c.StringToByteArray(splitAuthRES[6]));
                            if (c.VerifyCertificate(caCertificate, gatewayCertificate) == true)//kiểm tra chứng chỉ nhận từ gateway
                            {
                                Console.WriteLine("verify authorization response certificate true");
                                authorizationResponse = new AuthorizationResponse(splitAuthRES[0], splitAuthRES[1], splitAuthRES[2], splitAuthRES[3], splitAuthRES[4], splitAuthRES[5], splitAuthRES[6]);
                                if (authorizationResponse.verifyMessage() == true)
                                {
                                    Console.WriteLine("verify authorization response true");
                                    //lưu token
                                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                                    builder.DataSource = "localhost";
                                    builder.UserID = "sa";
                                    builder.Password = "123456";
                                    builder.InitialCatalog = "Bank";
                                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                                    {
                                        connection.Open();
                                        string sql;
                                        StringBuilder sb = new StringBuilder();
                                        sb.Clear();
                                        sb.Append("INSERT Token (TransID, SignToken, EncryptToken, EncryptKey) ");
                                        sb.Append("VALUES (@id, @sign, @token, @key);");
                                        sql = sb.ToString();
                                        using (SqlCommand command = new SqlCommand(sql, connection))
                                        {
                                            command.Parameters.AddWithValue("@id", authorizationResponse.getTransID());
                                            command.Parameters.AddWithValue("@sign", authorizationResponse.getSignToken());
                                            command.Parameters.AddWithValue("@token", authorizationResponse.getEncryptToken());
                                            command.Parameters.AddWithValue("@key", authorizationResponse.getEncryptKeyToken());
                                            int rowsAffected = command.ExecuteNonQuery();
                                        }
                                        connection.Close();
                                    }

                                    //tạo purchase response và gởi customer
                                    string[] messageRES = authorizationResponse.getMessage().Split(':');
                                    PurchaseResponse purchaseResponse = new PurchaseResponse(messageRES[0] + ":" + purchaseRequest.getRRPID() + ":" + messageRES[2] + ":" + messageRES[3]);
                                    c.send(purchaseResponse.ToMessage(), ref socket);
                                    //Console.WriteLine(purchaseResponse.getMessage());
                                    //tạo capture request gửi tới gateway
                                    string merchantCard = "012541AR09O5";
                                    string merchantCVV = "012345";
                                    string merchantDateValid = "25062019";

                                    //---->lấy token
                                    string signToken = "", encryptToken = "", encryptKeyToken = "";
                                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                                    {
                                        connection.Open();
                                        string sql;
                                        sql = "SELECT TransID, SignToken, EncryptToken, EncryptKey FROM Token;";
                                        using (SqlCommand command = new SqlCommand(sql, connection))
                                        {

                                            using (SqlDataReader sqlReader = command.ExecuteReader())
                                            {
                                                while (sqlReader.Read())
                                                {
                                                    if (sqlReader.GetString(0).CompareTo(authorizationResponse.getTransID()) == 0)
                                                    {
                                                        signToken = sqlReader.GetString(1);
                                                        encryptToken = sqlReader.GetString(2);
                                                        encryptKeyToken = sqlReader.GetString(3);
                                                    }
                                                }
                                            }
                                        }
                                        connection.Close();
                                    }
                                    CaptureRequest captureRequest = new CaptureRequest(purchaseRequest.getTransID(), merchantCard, merchantCVV, merchantDateValid, Convert.ToInt64(purchaseRequest.getTien()), gatewayPublicKey, signToken, encryptToken, encryptKeyToken);
                                    c.send(captureRequest.ToMessage(), ref client);

                                    //nhận capture response từ gateway
                                    receiveMessage = c.receive(ref client);
                                    string[] splitCaptureResponse = receiveMessage.Split('-');
                                    gatewayCertificate = new X509Certificate2(c.StringToByteArray(splitCaptureResponse[3]));
                                    if (c.VerifyCertificate(caCertificate, gatewayCertificate) == true)
                                    {
                                        Console.WriteLine("verify capture response certificate true");
                                        CaptureResponse captureResponse = new CaptureResponse(splitCaptureResponse[0], splitCaptureResponse[1], splitCaptureResponse[2], splitCaptureResponse[3]);
                                        if (captureResponse.verify() == true)
                                        {
                                            Console.WriteLine("verify capture response true");
                                            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))//lưu capture response
                                            {
                                                connection.Open();
                                                string sql;
                                                StringBuilder sb = new StringBuilder();
                                                sb.Clear();
                                                //lưu capture response
                                                sb.Append("INSERT LogCaptureResponse (SignMessage, EncryptMessage, EncryptKey) ");
                                                sb.Append("VALUES (@sign, @encrypt, @key);");
                                                sql = sb.ToString();
                                                using (SqlCommand command = new SqlCommand(sql, connection))
                                                {
                                                    command.Parameters.AddWithValue("@sign", captureResponse.getSignMessage());
                                                    command.Parameters.AddWithValue("@encrypt", captureResponse.getEncryptMessage());
                                                    command.Parameters.AddWithValue("@key", captureResponse.getEncryptKey());
                                                    int rowsAffected = command.ExecuteNonQuery();
                                                }
                                                //xác nhận tình trạng thanh toán của đơn hàng

                                                connection.Close();
                                            }
                                            builder.InitialCatalog = "QuanLyBanSach";
                                            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                                            {
                                                connection.Open();
                                                string sql;
                                                StringBuilder sb = new StringBuilder();
                                                sb.Clear();
                                                sb.Append("UPDATE DonHang SET DaThanhToan = @thanhtoan WHERE MaDonHang = @id");
                                                sql = sb.ToString();
                                                using (SqlCommand command = new SqlCommand(sql, connection))
                                                {
                                                    command.Parameters.AddWithValue("@thanhtoan", 1);
                                                    command.Parameters.AddWithValue("@id", maDH);
                                                    int rowsAffected = command.ExecuteNonQuery();
                                                }
                                                connection.Close();
                                            }
                                        }
                                    }

                                }

                            }
                        }
                        
                        client.Close();

                    }
                }                   
                
            }
            socket.Close();
        }
    }
}
