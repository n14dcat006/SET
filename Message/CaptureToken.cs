using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class CaptureToken
    {
        private string TransID;
        private string CardNumber;
        private double tien;
        public CaptureToken(string trans, string card, double soTien)
        {
            TransID = trans;
            CardNumber = card;
            tien = soTien;
        }
        public string ToMessage()
        {
            return TransID + ":" + CardNumber + ":" + tien;
        }
    }
}
