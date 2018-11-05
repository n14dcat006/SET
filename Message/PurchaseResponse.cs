using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class PurchaseResponse
    {
        private string messageRes;
        private string signMessage;
        private string MerchantCertificate;
        public string getCertificate()
        {
            return MerchantCertificate;
        }
        public PurchaseResponse(string message)
        {
            Common c = new Common();
            string merchantPrivateKey = File.ReadAllText("d:/file/MerchantPrivateKey.xml");
            signMessage = c.Sign(merchantPrivateKey, message);
            messageRes = message;
            X509Certificate2 certificate2 = new X509Certificate2("d:/file/merchant.crt");
            MerchantCertificate = c.ByteArrayToString(certificate2.GetRawCertData());
        }
        public string ToMessage()
        {
            return messageRes + "-" + signMessage + "-" + MerchantCertificate;
        }
        public PurchaseResponse(string s1, string s2, string s3)
        {
            messageRes = s1;
            signMessage = s2;
            MerchantCertificate = s3;
        }
        public string getMessage()
        {
            return messageRes;
        }
        public bool verify()
        {
            Common c = new Common();
            X509Certificate2 certificate2 = new X509Certificate2(c.StringToByteArray(MerchantCertificate));
            string merchantPublicKey = certificate2.GetRSAPublicKey().ToXmlString(false);
            return c.Verify(merchantPublicKey, signMessage, messageRes);
        }
    }
}
