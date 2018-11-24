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
namespace Issuer
{
    public class Program
    {
        static void Main(string[] args)
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 1236);
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
            X509Certificate2 caCertificate = new X509Certificate2("d:/file/ca.crt");
            X509Certificate2 gatewayCertificate;
            string issuerPrivateKey = File.ReadAllText("d:/file/IssuerPrivateKey.xml");
            X509Certificate2 issuerCertificate = new X509Certificate2("d:/file/issuer.crt");
            string sendMessage, receiveMessage;
            Common c = new Common();
            Socket socket = (Socket)sock;

            string message;
            //nhận authorization request từ gateway

            receiveMessage = c.receive(ref socket);
            string[] splitAuthReq = receiveMessage.Split('-');
            gatewayCertificate = new X509Certificate2(c.StringToByteArray(splitAuthReq[3]));
            if (c.VerifyCertificate(caCertificate,gatewayCertificate) == false)
            {
                Console.WriteLine("verify authorization request certificate from gateway false");
                message = "ERROR" + ":" + "4" + ":" + "xac thuc that bai";
                ForwardAuthorizationResponse forwardAuthorizationResponse = new ForwardAuthorizationResponse(message, issuerPrivateKey, c.ByteArrayToString(issuerCertificate.GetRawCertData()));
                c.send(forwardAuthorizationResponse.ToMessage(), ref socket);
            }
            else
            {
                Console.WriteLine("verify authorization request certificate from gateway true");
                ForwardAuthorizationRequest forwardAuthorization = new ForwardAuthorizationRequest(splitAuthReq[0], splitAuthReq[1], splitAuthReq[2], splitAuthReq[3]);
                if (forwardAuthorization.verify(issuerPrivateKey) == false)
                {
                    Console.WriteLine("verify authorization request from gateway false");
                    string message1 = "ERROR" + ":" + "4" + ":" + "xac thuc that bai";
                    ForwardAuthorizationResponse forwardAuthorizationResponse = new ForwardAuthorizationResponse(message1, issuerPrivateKey, c.ByteArrayToString(issuerCertificate.GetRawCertData()));
                    c.send(forwardAuthorizationResponse.ToMessage(), ref socket);
                }
                else
                {
                    Console.WriteLine("verify authorization request from gateway true");
                    string PI = forwardAuthorization.getPI(issuerPrivateKey);
                    string cardNumber, CVV, dateValid, transID;
                    long tien;
                    string[] splitPI = PI.Split(':');
                    transID = splitPI[0];
                    cardNumber = splitPI[3];
                    CVV = splitPI[4];
                    dateValid = splitPI[5];
                    tien = Convert.ToInt64(splitPI[6]);

                    //connect SQL server
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                    builder.DataSource = "localhost";
                    builder.UserID = "sa";
                    builder.Password = "123456";
                    builder.InitialCatalog = "Bank";
                    using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                    {
                        connection.Open();
                        int flag=0;//flag=0 tài khoản không chính xác - =1 số tiền không đủ - =2 tài khoản đúng và tiền còn đủ trong hạn mức
                        string sql;
                        StringBuilder sb = new StringBuilder();
                        sql = "SELECT CardNumber, CVV, DateValid, UsedMoney FROM Issuer;";
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            string a;
                            using (SqlDataReader sqlReader = command.ExecuteReader())
                            {
                                while (sqlReader.Read())
                                {
                                    a = sqlReader.GetString(2);

                                    if (cardNumber.Equals(sqlReader.GetString(0)) == true && CVV.Equals(sqlReader.GetString(1)) == true && dateValid.Equals(sqlReader.GetString(2)) == true)
                                    {
                                        Console.WriteLine(tien + " " + sqlReader.GetInt64(3));
                                        if (tien > sqlReader.GetInt64(3))
                                        {

                                            flag = 1;//tiền không đủ
                                        }
                                        else
                                        {
                                            flag = 2;//tiền đủ
                                        }
                                    }
                                }
                            }
                        }
                        if (flag == 0)
                        {
                            string s = "ERROR" + ":" + "2" + ":" + "tai khoan khong chinh xac";
                            ForwardAuthorizationResponse forwardAuthorizationResponse = new ForwardAuthorizationResponse(s, issuerPrivateKey, c.ByteArrayToString(issuerCertificate.GetRawCertData()));
                            c.send(forwardAuthorizationResponse.ToMessage(), ref socket);
                        }
                        else if (flag == 1)
                        {
                            string s = "ERROR" + ":" + "3" + ":" + "tien thanh toan vuot han muc";
                            ForwardAuthorizationResponse forwardAuthorizationResponse = new ForwardAuthorizationResponse(s, issuerPrivateKey, c.ByteArrayToString(issuerCertificate.GetRawCertData()));
                            c.send(forwardAuthorizationResponse.ToMessage(), ref socket);
                        }
                        else
                        {
                            //ghi PI vào log Isuuer
                            sb.Clear();
                            sb.Append("INSERT LogIssuer (TransID, CardNumber, Money, Paid) ");
                            sb.Append("VALUES (@trans, @cardid, @money, @paid);");
                            sql = sb.ToString();
                            using (SqlCommand command = new SqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@trans", transID);
                                command.Parameters.AddWithValue("@cardid", cardNumber);
                                command.Parameters.AddWithValue("@money", tien);
                                command.Parameters.AddWithValue("@paid", 0);
                                int rowsAffected = command.ExecuteNonQuery();
                            }
                            //gửi forward response
                            string issuerCert = c.ByteArrayToString(issuerCertificate.GetRawCertData());
                            ForwardAuthorizationResponse authorizationResponse = new ForwardAuthorizationResponse(transID, 1, cardNumber, issuerPrivateKey, issuerCert);
                            c.send(authorizationResponse.ToMessage(), ref socket);

                            //nhận capture request từ gateway
                            receiveMessage = c.receive(ref socket);
                            string[] splitCapture = receiveMessage.Split('-');
                            gatewayCertificate = new X509Certificate2(c.StringToByteArray(splitCapture[6]));
                            if (c.VerifyCertificate(caCertificate, gatewayCertificate) == false)
                            {
                                Console.WriteLine("verify capture request certificate from gateway false");
                                string s = "ERROR" + ":" + "4" + ":" + "xac thuc that bai";
                                s = s + "-" + c.Sign(issuerPrivateKey, s) + "-" + c.ByteArrayToString(issuerCertificate.GetRawCertData());
                                c.send(s, ref socket);
                            }
                            else
                            {
                                Console.WriteLine("verify capture request certificate from gateway true");
                                string captureRequest = getToken(receiveMessage);//thông tin tài khoản customer
                                if (captureRequest == null)
                                {
                                    string message1 = "ERROR" + ":" + "4" + ":" + "xac thuc that bai";
                                    message1 = message1 + "-" + c.Sign(issuerPrivateKey, message1) + "-" + c.ByteArrayToString(issuerCertificate.GetRawCertData());
                                    c.send(message1, ref socket);
                                }
                                else
                                {
                                    string customerCardNumber;
                                    long soTien;
                                    string[] splitCaptureRequest = captureRequest.Split(':');
                                    transID = splitCaptureRequest[0];
                                    customerCardNumber = splitCaptureRequest[1];
                                    soTien = Convert.ToInt64(splitCaptureRequest[2]);

                                    //nhập dữ liệu thanh toán vào sql server
                                    using (SqlConnection connection1 = new SqlConnection(builder.ConnectionString))
                                    {
                                        connection1.Open();
                                        string sql1;
                                        StringBuilder sb1 = new StringBuilder();
                                        sb1.Clear();
                                        sb1.Append("UPDATE LogIssuer SET Paid = @paid WHERE TransID = @id");
                                        sql1 = sb1.ToString();
                                        using (SqlCommand command = new SqlCommand(sql1, connection1))
                                        {
                                            command.Parameters.AddWithValue("@id", transID);
                                            command.Parameters.AddWithValue("@paid", 1);
                                            int rowsAffected = command.ExecuteNonQuery();
                                        }
                                        long tienBanDau = 0;
                                        sql1 = "SELECT CardNumber, UsedMoney FROM Issuer;";
                                        using (SqlCommand command = new SqlCommand(sql1, connection1))
                                        {
                                            using (SqlDataReader sqlReader = command.ExecuteReader())
                                            {
                                                while (sqlReader.Read())
                                                {
                                                    if (cardNumber.Equals(sqlReader.GetString(0)) == true)
                                                    {
                                                        tienBanDau = sqlReader.GetInt64(1);
                                                    }

                                                }
                                            }
                                        }
                                        sb1.Clear();
                                        sb1.Append("UPDATE Issuer SET UsedMoney = @tien WHERE CardNumber = @id");
                                        sql1 = sb1.ToString();
                                        using (SqlCommand command = new SqlCommand(sql1, connection1))
                                        {
                                            command.Parameters.AddWithValue("@tien", tienBanDau - soTien );
                                            command.Parameters.AddWithValue("@id", customerCardNumber);
                                            int rowsAffected = command.ExecuteNonQuery();
                                        }
                                        connection1.Close();
                                        connection.Close();
                                    }

                                    //send message to acquirer                            
                                    sendMessage = splitCapture[0] + "-" + splitCapture[1] + "-" + splitCapture[2] + "-" + splitCapture[6];
                                    IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1237);
                                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    client.Connect(iep);
                                    c.send(sendMessage, ref client);

                                    //nhận message từ acquirer
                                    receiveMessage = c.receive(ref client);
                                    string[] splitAcquirer = receiveMessage.Split('-');
                                    X509Certificate2 acquirerCertificate = new X509Certificate2(c.StringToByteArray(splitAcquirer[2]));
                                    if (c.VerifyCertificate(caCertificate, acquirerCertificate) == true)
                                    {
                                        Console.WriteLine("verify capture response certificate from acquirer true");
                                        string acquirerPublicKey = acquirerCertificate.GetRSAPublicKey().ToXmlString(false);
                                        if (c.Verify(acquirerPublicKey, splitAcquirer[1], splitAcquirer[0]) == true)
                                        {
                                            Console.WriteLine("verify capture response from acquirer true");
                                            message = splitAcquirer[0];
                                            c.send(message + "-" + c.Sign(issuerPrivateKey, message) + "-" + issuerCert, ref socket);
                                        }
                                    }
                                    //client.Close();
                                }
                            }
                        }

                        
                    }

                                       
                }                
            }
            
            socket.Close();
        }
        static string getToken(string receiveMessage)
        {
            Common c = new Common();
            string issuerPrivateKey = File.ReadAllText("d:/file/IssuerPrivateKey.xml");
            string[] split = receiveMessage.Split('-');
            X509Certificate2 gatewayCertificate = new X509Certificate2(c.StringToByteArray(split[6]));
            string gatewayPublicKey = gatewayCertificate.GetRSAPublicKey().ToXmlString(false);
            string signToken = split[3], encryptToken = split[4], encryptKeyToken = split[5];
            string key = c.DecryptionRSA(issuerPrivateKey, encryptKeyToken);
            string token = c.DecryptDES(encryptToken, key);
            if (c.Verify(gatewayPublicKey, signToken, token) == true)
            {
                Console.WriteLine("verify capture request from gateway true");
                return token;
            }
            else
            {
                Console.WriteLine("verify capture request from gateway false");
                return null;
            }
        }
    }
}
