using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message;
namespace Message
{
    public class PaymentInstructions
    {
        private string TransID;
        private string RRPID;
        private string BrandID;
        private string cardNumber, CVV, dateValid;
        private long tien;
        public PaymentInstructions(string maCard, string cvv, string ngay, long tongTien, string trans, string brand)
        {
            Common c = new Common();
            TransID = trans;
            RRPID = c.Random(2);
            BrandID = brand;
            cardNumber = maCard;
            CVV = cvv;
            dateValid = ngay;
            tien = tongTien;
        }
        public string getTransID()
        {
            return TransID;
        }
        public PaymentInstructions(string s1, string s2, string s3, string s4, string s5, string s6, long d)
        {
            TransID = s1;
            RRPID = s2;
            BrandID = s3;
            cardNumber = s4;
            CVV = s5;
            dateValid = s6;
            tien = d;
        }
        public string PIToString()
        {
            return TransID + ":" + RRPID + ":" + BrandID + ":" + cardNumber + ":" + CVV + ":" + dateValid + ":" + tien;
        }
        public void setRRPID(string pid)
        {
            RRPID = pid;
        }
        public string getRRPID()
        {
            return RRPID;
        }
        public string getCardNumber()
        {
            return cardNumber;
        }
        public double getTien()
        {
            return tien;
        }
    }
}
