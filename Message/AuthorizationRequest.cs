using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace Message
{
    public class AuthorizationRequest
    {
        private string CustomerDESMessage;
        private string CustomerDigitalEnvelope;
        private string MerchantDESMessage;
        private string MerchantDigitalEnvelope;
        private string MerchantSignMessage;
        private string CustomerCertificate;
        private string MerchantCertificate;
        public AuthorizationRequest(string TransID, double tongTien, string merchantPriKey, string gatewayPubKey, string custCert, string merchantCert, string customerDES, string customerEncryptKey)
        {
            Common c = new Common();
            string initREQ = TransID + ":" + tongTien;
            MerchantSignMessage = c.Sign(merchantPriKey, initREQ);
            string key = c.Random(8);
            MerchantDESMessage = c.EncryptDES(initREQ, key);
            MerchantDigitalEnvelope = c.EncryptRSA(gatewayPubKey, key);
            CustomerCertificate = custCert;
            MerchantCertificate = merchantCert;
            CustomerDESMessage = customerDES;
            CustomerDigitalEnvelope = customerEncryptKey;
        }
        public string ToMessage()
        {
            return MerchantDESMessage + "-" + MerchantSignMessage + "-" + CustomerDESMessage + "-" + CustomerDigitalEnvelope + "-" + CustomerCertificate + "-" + MerchantCertificate + "-" + MerchantDigitalEnvelope;
        }
        public string getMerchantCertificate()
        {
            return MerchantCertificate;
        }
        public string getPI(string gatewayPriKey)
        {
            Common c = new Common();
            X509Certificate2 customerCert = new X509Certificate2(c.StringToByteArray(CustomerCertificate));
            string customerKey = c.DecryptionRSA(gatewayPriKey, CustomerDigitalEnvelope);
            string[] tam = c.DecryptDES(CustomerDESMessage, customerKey).Split('-');
            string PI = tam[0];
            return PI;
        }
        public AuthorizationRequest(string s1, string s2, string s3, string s4, string s5, string s6, string s7)
        {
            MerchantDESMessage = s1;
            MerchantSignMessage = s2;
            CustomerDESMessage = s3;
            CustomerDigitalEnvelope = s4;
            CustomerCertificate = s5;
            MerchantCertificate = s6;
            MerchantDigitalEnvelope = s7;
        }
        public bool Verify(string gatewayPriKey)
        {
            Common c = new Common();
            X509Certificate2 merchantCert = new X509Certificate2(c.StringToByteArray(MerchantCertificate));
            X509Certificate2 customerCert = new X509Certificate2(c.StringToByteArray(CustomerCertificate));
            //xác thực merchant message
            string merchantKey = c.DecryptionRSA(gatewayPriKey, MerchantDigitalEnvelope);
            string merchantRequest = c.DecryptDES(MerchantDESMessage, merchantKey);
            string merchantPublicKey = merchantCert.GetRSAPublicKey().ToXmlString(false);
            string customerPubicKey = customerCert.GetRSAPublicKey().ToXmlString(false);
            bool merchantVerify = c.Verify(merchantPublicKey, MerchantSignMessage, merchantRequest);
            if (merchantVerify == false) return false;
            //xác thực customer message
            string customerKey = c.DecryptionRSA(gatewayPriKey, CustomerDigitalEnvelope);
            string[] tam = c.DecryptDES(CustomerDESMessage, customerKey).Split('-');
            string PI = tam[0];            
            string DualSignature = tam[1];
            string OIMD = tam[2];
            string POMD = c.Hash(c.Hash(PI) + OIMD);
            bool customerVerify = c.Verify(customerPubicKey, DualSignature, POMD);
            if (customerVerify == false) return false;
            //so sánh TransID của PI và TransID của AuthReq
            string[] splitPI = PI.Split(':');
            PaymentInstructions paymentInstructions = new PaymentInstructions(splitPI[0], splitPI[1], splitPI[2], splitPI[3], splitPI[4], splitPI[5], Convert.ToInt64(splitPI[6]));
            if (merchantRequest.Split(':')[0].CompareTo(paymentInstructions.getTransID()) == 0)
            {
                Console.WriteLine(paymentInstructions.getTransID());
                return true;

            }
            else return false;
        }
    }
}
