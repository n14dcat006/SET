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
namespace Acquirer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 1237);
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
            X509Certificate2 acquirerCertificate = new X509Certificate2("d:/file/acquirer.crt");
            string privateKeyAcquirer = File.ReadAllText("d:/file/AcquirerPrivateKey.xml");
            X509Certificate2 caCertificate = new X509Certificate2("d:/file/ca.crt");
            string receiveMessage;
            Common c = new Common();
            Socket socket = (Socket)sock;

            //nhận message từ issuer
            receiveMessage = c.receive(ref socket);
            string[] splitMessage = receiveMessage.Split('-');
            X509Certificate2 gatewayCertificate = new X509Certificate2(c.StringToByteArray(splitMessage[3]));
            if (c.VerifyCertificate(caCertificate,gatewayCertificate) == false)
            {
                Console.WriteLine("verify capture request certificate from issuer false");
                string s = "ERROR" + ":" + "4" + ":" + "xac thuc that bai";
                s = s + "-" + c.Sign(privateKeyAcquirer, s) + "-" + c.ByteArrayToString(acquirerCertificate.GetRawCertData());
                c.send(s, ref socket);
            }
            else
            {
                Console.WriteLine("verify capture request certificate from issuer true");
                string captureRequest = getCaptureRequest(receiveMessage);
                if (captureRequest == null)
                {

                    string s = "ERROR" + ":" + "4" + ":" + "xac thuc that bai";
                    s = s + "-" + c.Sign(privateKeyAcquirer, s) + "-" + c.ByteArrayToString(acquirerCertificate.GetRawCertData());
                    c.send(s, ref socket);
                }
                else
                {
                    string TransID, merchantCardNumber, merchantCVV, merchantDateValid;
                    long tien;
                    string[] split = captureRequest.Split(':');
                    TransID = split[0];
                    merchantCardNumber = split[2];
                    merchantCVV = split[3];
                    merchantDateValid = split[4];
                    tien = Convert.ToInt64(split[5]);

                    //connect SQL server
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
                        long tienBanDau = 0;
                        sql = "SELECT CardNumber, Money FROM Acquirer;";
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            using (SqlDataReader sqlReader = command.ExecuteReader())
                            {
                                while (sqlReader.Read())
                                {
                                    if (merchantCardNumber.Equals(sqlReader.GetString(0)) == true)
                                    {
                                        tienBanDau = sqlReader.GetInt64(1);
                                    }

                                }
                            }
                        }
                        sb.Clear();
                        sb.Append("UPDATE Acquirer SET Money = @tien WHERE CardNumber = @id and CVV = @cvv and DateValid = @date");
                        sql = sb.ToString();
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", merchantCardNumber);
                            command.Parameters.AddWithValue("@cvv", merchantCVV);
                            command.Parameters.AddWithValue("@date", merchantDateValid);
                            command.Parameters.AddWithValue("@tien", tien);
                            int rowsAffected = command.ExecuteNonQuery();
                        }

                        sb.Clear();
                        sb.Append("INSERT LogAcquirer (TransID, CardNumber, Money) ");
                        sb.Append("VALUES (@id, @card, @tien);");
                        sql = sb.ToString();
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", TransID);
                            command.Parameters.AddWithValue("@card", merchantCardNumber);
                            command.Parameters.AddWithValue("@tien", tien);
                            int rowsAffected = command.ExecuteNonQuery();
                        }
                        connection.Close();
                    }

                    //send response to issuer
                    string message = TransID + ":" + "1" + ":" + "thanh toan thanh cong";
                    c.send(message + "-" + c.Sign(privateKeyAcquirer, message) + "-" + c.ByteArrayToString(acquirerCertificate.GetRawCertData()), ref socket);
                }                
            }
            socket.Close();
        }
        static string getCaptureRequest(string receiveMessage)
        {
            Common c = new Common();
            string acquirerPrivateKey = File.ReadAllText("d:/file/AcquirerPrivateKey.xml");
            string[] split = receiveMessage.Split('-');
            X509Certificate2 gatewayCertificate = new X509Certificate2(c.StringToByteArray(split[3]));
            string gatewayPublicKey = gatewayCertificate.GetRSAPublicKey().ToXmlString(false);
            string signCaptureRequest = split[0], encryptCaptureRequest = split[1], encryptKeyCapture = split[2];
            string key = c.DecryptionRSA(acquirerPrivateKey, encryptKeyCapture);
            string captureResquest = c.DecryptDES(encryptCaptureRequest, key);
            if (c.Verify(gatewayPublicKey, signCaptureRequest, captureResquest) == true)
            {
                Console.WriteLine("verify capture request from issuer true");
                return captureResquest;
            }
            else
            {
                Console.WriteLine("verify capture request from issuer false");
                return null;
            }
        }
    }
}
