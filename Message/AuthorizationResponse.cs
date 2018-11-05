using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class AuthorizationResponse
    {
        private string signMessage;
        private string encryptDESMessage;
        private string encryptRSAKeyMessage;
        private string signCaptureToken;
        private string encryptDESToken;
        private string encryptRSAKey;
        private string gatewayCertificate;
        public AuthorizationResponse(string issuerMessage, string publicKeyMerchant)
        {
            Common c = new Common();
            string gatewayPrivateKey = File.ReadAllText("d:/file/GatewayPrivateKey.xml");
            signMessage = c.Sign(gatewayPrivateKey, issuerMessage);
            string key = c.Random(8);
            encryptDESMessage = c.EncryptDES(issuerMessage, key);
            encryptRSAKeyMessage = c.EncryptRSA(publicKeyMerchant, key);
            X509Certificate2 certificate2 = new X509Certificate2("d:/file/gateway.crt");
            gatewayCertificate = c.ByteArrayToString(certificate2.GetRawCertData());
        }
        public AuthorizationResponse(string s1, string s2, string s3, string s4, string s5, string s6, string s7)
        {
            signMessage = s1;
            encryptDESMessage = s2;
            encryptRSAKeyMessage = s3;
            signCaptureToken = s4;
            encryptDESToken = s5;
            encryptRSAKey = s6;
            gatewayCertificate = s7;
        }
        public AuthorizationResponse(string s1, string s2, string s3, string s4)
        {
            signMessage = s1;
            encryptDESMessage = s2;
            encryptRSAKeyMessage = s3;
            gatewayCertificate = s4;
        }
        public string ToMessage()
        {
            return signMessage + "-" + encryptDESMessage + "-" + encryptRSAKeyMessage +"-"+ signCaptureToken + "-" + encryptDESToken + "-" + encryptRSAKey + "-" + gatewayCertificate;
        }
        public string ToMessageNoToken()
        {
            return signMessage + "-" + encryptDESMessage + "-" + encryptRSAKeyMessage + "-" + gatewayCertificate;
        }
        public bool verifyMessage()
        {
            Common c = new Common();
            X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(gatewayCertificate));
            string gatewayPublicKey = certificate2.GetRSAPublicKey().ToXmlString(false);
            string merchantPrivateKey = File.ReadAllText("d:/file/MerchantPrivateKey.xml");
            string key = c.DecryptionRSA(merchantPrivateKey, encryptRSAKeyMessage);
            string message = c.DecryptDES(encryptDESMessage, key);
            return c.Verify(gatewayPublicKey, signMessage, message);
        }
        public string getSignToken()
        {
            return signCaptureToken;
        } 
        public string getEncryptToken()
        {
            return encryptDESToken;
        }
        public string getEncryptKeyToken()
        {
            return encryptRSAKey;
        }
        public string getMessage()
        {
            Common c = new Common();
            X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(gatewayCertificate));
            string gatewayPublicKey = certificate2.GetRSAPublicKey().ToXmlString(false);
            string merchantPrivateKey = File.ReadAllText("d:/file/MerchantPrivateKey.xml");
            string key = c.DecryptionRSA(merchantPrivateKey, encryptRSAKeyMessage);
            string message = c.DecryptDES(encryptDESMessage, key);
            return message;
        }
        public string getTransID()
        {
            Common c = new Common();
            X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(gatewayCertificate));
            string gatewayPublicKey = certificate2.GetRSAPublicKey().ToXmlString(false);
            string merchantPrivateKey = File.ReadAllText("d:/file/MerchantPrivateKey.xml");
            string key = c.DecryptionRSA(merchantPrivateKey, encryptRSAKeyMessage);
            string message = c.DecryptDES(encryptDESMessage, key);
            string[] split = message.Split(':');
            return split[0];
        }
        public void setCaptureToken(string token)
        {
            Common c = new Common();
            string gatewayPrivateKey = File.ReadAllText("d:/file/GatewayPrivateKey.xml");
            string gatewayPublicKey = File.ReadAllText("d:/file/GatewayPublicKey.xml");
            signCaptureToken = c.Sign(gatewayPrivateKey, token);
            string key = c.Random(8);
            encryptDESToken = c.EncryptDES(token, key);
            encryptRSAKey = c.EncryptRSA(gatewayPublicKey, key);
        }
    }
}
