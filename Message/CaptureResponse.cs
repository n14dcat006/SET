using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class CaptureResponse
    {
        private string signMessage;
        private string encryptMessage;
        private string encryptKey;
        private string certificate;
        public CaptureResponse(string message, string merchantPublicKey)
        {
            Common c = new Common();
            X509Certificate2 certificate2 = new X509Certificate2("d:/file/gateway.crt");
            string privateKey = File.ReadAllText("d:/file/GatewayPrivateKey.xml");
            string key = c.Random(8);
            signMessage = c.Sign(privateKey, message);
            encryptMessage = c.EncryptDES(message, key);
            encryptKey = c.EncryptRSA(merchantPublicKey, key);
            certificate = c.ByteArrayToString(certificate2.GetRawCertData());
        }
        public CaptureResponse(string s1, string s2, string s3, string s4)
        {
            signMessage = s1;
            encryptMessage = s2;
            encryptKey = s3;
            certificate = s4;
        }
        public bool verify()
        {
            Common c = new Common();
            X509Certificate2 gatewayCertificate = new X509Certificate2(c.StringToByteArray(certificate));
            string publicKeyGateway = gatewayCertificate.GetRSAPublicKey().ToXmlString(false);
            string merchantPrivateKey = File.ReadAllText("d:/file/MerchantPrivateKey.xml");
            string key = c.DecryptionRSA(merchantPrivateKey, encryptKey);
            string message = c.DecryptDES(encryptMessage, key);
            return c.Verify(publicKeyGateway, signMessage, message);
        }
        public string ToMessage()
        {
            return signMessage + "-" + encryptMessage + "-" + encryptKey + "-" + certificate;
        }
        public string getSignMessage()
        {
            return signMessage;
        }
        public string getEncryptMessage()
        {
            return encryptMessage;
        }
        public string getEncryptKey()
        {
            return encryptKey;
        }
    }
}
