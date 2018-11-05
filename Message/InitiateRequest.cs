using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message;
namespace Message
{
    public class InitiateRequest
    {
        private string RRPID;
        private string language;
        private string LIDC;
        private string LIDM;
        private string BrandID;
        public string getRRPID()
        {
            return RRPID;
        }
        public string getLanguage()
        {
            return language;
        }
        public string getLIDC()
        {
            return LIDC;
        }
        public string getLIDM()
        {
            return LIDM;
        }
        public string getBrandID()
        {
            return BrandID;
        }
        public InitiateRequest()
        {
            Common c = new Common();
            RRPID = c.Random(2);
            language = "vn";
            LIDC = "customer";
            LIDM = "";
            BrandID = "VISA";
        }
        public InitiateRequest(string a, string b, string c, string d, string e)
        {
            RRPID = a;
            language = b;
            LIDC = c;
            LIDM = d;
            BrandID = e;
        }

        public string ToMessage()
        {
            string kq = RRPID + "-" + language+"-"+LIDC+"-"+LIDM+"-"+BrandID;
            return kq;
        }
    }
}
