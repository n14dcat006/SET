using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Message;
namespace Message
{
    public class PurchaseRequest
    {
        private string CustomerToGateway;
        private string CustomerDigitalEnvelope;
        private string PIMD;
        private string OI;
        private string DualSignature;
        private string customerCertificate;
        public PurchaseRequest(string oi, string pi, string priKeyCust, string pubKeyGateway, string customerCert)
        {
            Common c = new Common();
            OI = oi;
            string PI = pi;
            string OIMD = c.Hash(OI);
            PIMD = c.Hash(PI);
            string POMD = c.Hash(PIMD + OIMD);
            DualSignature = c.Sign(priKeyCust, POMD);
            string key = c.Random(8);
            CustomerToGateway = c.EncryptDES(PI + "-" + DualSignature + "-" + OIMD, key);
            CustomerDigitalEnvelope = c.EncryptRSA(pubKeyGateway, key);
            customerCertificate = customerCert;
        }
        public string ToMessage()
        {
            return CustomerToGateway + "-" + CustomerDigitalEnvelope + "-" + PIMD + "-" + OI + "-" + DualSignature + "-" + customerCertificate;
        }
        public PurchaseRequest(string a, string b, string c, string d, string e, string f)
        {
            CustomerToGateway = a;
            CustomerDigitalEnvelope = b;
            PIMD = c;
            OI = d;
            DualSignature = e;
            customerCertificate = f;
        }
        public bool verify()
        {
            Common c = new Common();
            X509Certificate2 custCert = new X509Certificate2(c.StringToByteArray(customerCertificate));
            string custPublicKey = custCert.GetRSAPublicKey().ToXmlString(false);
            string OIMD = c.Hash(OI);
            string POMD = c.Hash(PIMD + OIMD);
            return c.Verify(custPublicKey, DualSignature, POMD);
        }
        public string getTransID()
        {
            string[] tam = OI.Split(':');
            return tam[0];

        }
        public string getTien()
        {
            string[] tam = OI.Split(':');
            return tam[6];
        }
        public string getBrandID()
        {
            string[] tam = OI.Split(':');
            return tam[2];
        }
        public string getRRPID()
        {
            string[] splitOI = OI.Split(':');
            return splitOI[1];
        }
        public int getMaDH()
        {
            string[] splitOI = OI.Split(':');
            return Convert.ToInt32(splitOI[3]);
        }
        public string getMessageToGateway()
        {
            return CustomerToGateway;
        }
        public string getDigitalEnvelop()
        {
            return CustomerDigitalEnvelope;
        }
        public string getCustommerCertificate()
        {
            return customerCertificate;
        }
    }
}
