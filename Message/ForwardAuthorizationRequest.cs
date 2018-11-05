using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class ForwardAuthorizationRequest
    {
        private string signRequest;
        private string encryptDESRequest;
        private string encryptKey;
        private string gatewayCertificate;
        public ForwardAuthorizationRequest(string PI, string IssuerPublicKey)
        {
            Common c = new Common();
            X509Certificate2 gatewayCert = new X509Certificate2("d:/file/gateway.crt");
            gatewayCertificate = c.ByteArrayToString(gatewayCert.GetRawCertData());
            string gatewayPrivateKey = File.ReadAllText("d:/file/GatewayPrivateKey.xml");
            signRequest = c.Sign(gatewayPrivateKey, PI);
            string key = c.Random(8);
            encryptDESRequest = c.EncryptDES(PI, key);
            encryptKey = c.EncryptRSA(IssuerPublicKey, key);
        }
        public ForwardAuthorizationRequest(string s1, string s2, string s3, string s4)
        {
            signRequest = s1;
            encryptDESRequest = s2;
            encryptKey = s3;
            gatewayCertificate = s4;
        }
        public bool verify(string issuerPriKey)
        {
            Common c = new Common();
            string key = c.DecryptionRSA(issuerPriKey, encryptKey);
            string request = c.DecryptDES(encryptDESRequest, key);
            X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(gatewayCertificate));
            string gatewayPublicKey = certificate2.GetRSAPublicKey().ToXmlString(false);
            return c.Verify(gatewayPublicKey, signRequest, request);
        }
        public string getPI(string issuerPriKey)
        {
            Common c = new Common();
            string key = c.DecryptionRSA(issuerPriKey, encryptKey);
            string PI = c.DecryptDES(encryptDESRequest, key);
            return PI;
        }
        public string ToMessage()
        {
            return signRequest + "-" + encryptDESRequest + "-" + encryptKey+"-"+gatewayCertificate;
        }
    }
}
