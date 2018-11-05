using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message;
namespace Message
{
    public class InitiateResponse
    {
        private string TransID;
        private string RRPID;
        private string BrandID;
        private string merchantCertificate;
        private string gatewayCertificate;
        public string getTransID()
        {
            return TransID;
        }
        public string getBrandID()
        {
            return BrandID;
        }

        public InitiateResponse(string lidc, string lang,string pid,string brand, string merchant, string gateway)
        { 
            Common c = new Common();
            string LIDC, LIDM, XID, PReqDate, Language;
            LIDC = lidc;
            LIDM = "merchant";
            XID = c.Random(5);
            PReqDate = DateTime.Now.ToString("yyyyMMdd");
            Language = lang;
            TransID = LIDC + LIDM + XID + PReqDate + Language;
            RRPID = pid;
            BrandID = brand;
            merchantCertificate = merchant;
            gatewayCertificate = gateway;
        }
        public InitiateResponse(string a, string b, string c)
        {
            TransID = a;
            RRPID = b;
            BrandID = c;
        }
        public string ToMessage(string privateKey)
        {
            string s = TransID+":"+RRPID+":"+BrandID;
            Common c=new Common();
            string kq = s+"-"+c.Sign(privateKey, s)+"-"+merchantCertificate+"-"+gatewayCertificate;
            return kq;
        }
    }
}
