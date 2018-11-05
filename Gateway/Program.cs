using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Message;
namespace Gateway
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 1235);
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
            string gatewayPrivateKey = File.ReadAllText("d:/file/gatewayPrivateKey.xml");
            X509Certificate2 caCertificate = new X509Certificate2("d:/file/ca.crt");
            X509Certificate2 gatewayCertificate = new X509Certificate2("d:/file/gateway.crt");
            X509Certificate2 issuerCertificate = new X509Certificate2("d:/file/issuer.crt");
            X509Certificate2 customerCertificate, merchantCertificate;
            string sendMessage;
            Common c = new Common();
            Socket socket = (Socket)sock;

            //nhận auth request
            string receiveMessage = c.receive(ref socket);
            string[] tam = receiveMessage.Split('-');
            customerCertificate = new X509Certificate2(c.StringToByteArray(tam[4]));
            merchantCertificate = new X509Certificate2(c.StringToByteArray(tam[5]));
            if (c.VerifyCertificate(caCertificate,customerCertificate) == false || c.VerifyCertificate(caCertificate,merchantCertificate) == false)
            {
                Console.WriteLine("verify authorization request certificate false");
                string message = "ERROR" + "-" + 3 + "-" + "xac thuc that bai";
                c.send(message, ref socket);
            }
            else
            {
                AuthorizationRequest authorizationRequest = new AuthorizationRequest(tam[0], tam[1], tam[2], tam[3], tam[4], tam[5], tam[6]);
                if (authorizationRequest.Verify(gatewayPrivateKey) == false)
                {
                    Console.WriteLine("verify authorization request false");
                    string s = "ERROR" + "-" + 3 + "-" + "xac thuc that bai";
                    c.send(s, ref socket);
                }
                else
                {
                    Console.WriteLine("verify authorization request true");
                    //chuyển auth request đến issuer
                    string issuerPublicKey = issuerCertificate.GetRSAPublicKey().ToXmlString(false);
                    string PI = authorizationRequest.getPI(gatewayPrivateKey);
                    string[] splitPI = PI.Split(':');
                    PaymentInstructions paymentInstructions = new PaymentInstructions(splitPI[0], splitPI[1], splitPI[2], splitPI[3], splitPI[4], splitPI[5], Convert.ToInt64(splitPI[6]));
                    string RRPID = paymentInstructions.getRRPID();
                    paymentInstructions.setRRPID(c.Random(2));
                    ForwardAuthorizationRequest forwardAuthorization = new ForwardAuthorizationRequest(paymentInstructions.PIToString(), issuerPublicKey);
                    sendMessage = forwardAuthorization.ToMessage();

                    //kết nối issuer
                    IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1236);
                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client.Connect(iep);

                    c.send(sendMessage, ref client);

                    //nhận kq response từ issuer
                    receiveMessage = c.receive(ref client);
                    string[] splitRES = receiveMessage.Split('-');
                    issuerCertificate = new X509Certificate2(c.StringToByteArray(splitRES[2]));
                    if (c.VerifyCertificate(caCertificate,issuerCertificate) == true)
                    {
                        Console.WriteLine("verify authorization response certificate from issuer true");
                        issuerPublicKey = issuerCertificate.GetRSAPublicKey().ToXmlString(false);                        
                        bool verifyRES = c.Verify(issuerPublicKey, splitRES[1], splitRES[0]);
                        if (verifyRES == true)
                        {
                            Console.WriteLine("verify authorization response from issuer true");
                            //tạo và gửi auth response
                            string[] splitIssuerRES = splitRES[0].Split(':');
                            //kiểm tra response từ issuer có ERROR hay không
                            if (splitIssuerRES[0].CompareTo("ERROR") == 0)
                            {
                                string message = splitIssuerRES[0] + ":" + RRPID + ":" + splitIssuerRES[1] + ":" + splitIssuerRES[2];
                                X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(authorizationRequest.getMerchantCertificate()));
                                string publicKeyMerchant = certificate2.GetRSAPublicKey().ToXmlString(false);
                                AuthorizationResponse authorizationResponse = new AuthorizationResponse(message, publicKeyMerchant);
                                c.send(authorizationResponse.ToMessageNoToken(), ref socket);
                            }
                            else
                            {
                                string message = splitIssuerRES[0] + ":" + RRPID + ":" + splitIssuerRES[1] + ":" + splitIssuerRES[2];
                                CaptureToken token = new CaptureToken(paymentInstructions.getTransID(), paymentInstructions.getCardNumber(), paymentInstructions.getTien());
                                X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(authorizationRequest.getMerchantCertificate()));
                                string publicKeyMerchant = certificate2.GetRSAPublicKey().ToXmlString(false);
                                AuthorizationResponse authorizationResponse = new AuthorizationResponse(message, publicKeyMerchant);
                                authorizationResponse.setCaptureToken(token.ToMessage());
                                c.send(authorizationResponse.ToMessage(), ref socket);
                                //nhận capture request
                                receiveMessage = c.receive(ref socket);
                                string[] splitCapture = receiveMessage.Split('-');
                                merchantCertificate = new X509Certificate2(c.StringToByteArray(splitCapture[6]));
                                if (c.VerifyCertificate(caCertificate, merchantCertificate) == false)
                                {
                                    Console.WriteLine("verify capture request certificate false");
                                    string message1 = "ERROR" + "-" + 3 + "-" + "xac thuc that bai";
                                    c.send(message1, ref socket);
                                }
                                else
                                {
                                    Console.WriteLine("verify capture request certificate true");
                                    CaptureRequest captureRequest = new CaptureRequest(splitCapture[0], splitCapture[1], splitCapture[2], splitCapture[3], splitCapture[4], splitCapture[5], splitCapture[6]);
                                    if (captureRequest.Verify() == false)
                                    {
                                        Console.WriteLine("verify capture request false");
                                        message = "ERROR" + "-" + 3 + "-" + "xac thuc that bai";
                                        c.send(message, ref socket);
                                    }
                                    else
                                    {
                                        Console.WriteLine("verify capture request true");
                                        //chuyển capture request tới issuer
                                        sendMessage = captureRequest.messageToIssuer();
                                        c.send(sendMessage, ref client);

                                        //nhận message từ issuer
                                        receiveMessage = c.receive(ref client);
                                        string[] splitCaptureRES = receiveMessage.Split('-');
                                        issuerCertificate = new X509Certificate2(c.StringToByteArray(splitCaptureRES[2]));
                                        if (c.VerifyCertificate(caCertificate, issuerCertificate) == true)
                                        {
                                            Console.WriteLine("verify capture response certificate from issuer true");
                                            issuerCertificate = new X509Certificate2(c.StringToByteArray(splitCaptureRES[2]));
                                            issuerPublicKey = issuerCertificate.GetRSAPublicKey().ToXmlString(false);
                                            if (c.Verify(issuerPublicKey, splitCaptureRES[1], splitCaptureRES[0]) == true)
                                            {
                                                Console.WriteLine("verify capture response from issuer true");
                                                //tạo capture response gừi tới merchant
                                                string[] split = splitCaptureRES[0].Split(':');
                                                message = split[0] + ":" + captureRequest.getRRPID() + ":" + split[1] + ":" + split[2];
                                                CaptureResponse captureResponse = new CaptureResponse(message, publicKeyMerchant);
                                                c.send(captureResponse.ToMessage(), ref socket);
                                            }
                                        }
                                        client.Close();
                                    }
                                }
                            }
                                                   
                        }                        
                    }                    
                }                
            }
            
            socket.Close();
        }
    }
}
