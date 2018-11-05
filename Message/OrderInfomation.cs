using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message;
namespace Message
{
    public class OrderInfomation
    {
        private string TransIDs, RRPID, BrandID, ngayDH;
        private int maDH, maKH;
        private long tien;
        public OrderInfomation(int maDonHang, int maKhachhang, string ngay, string trans, string brand, long tongTien)
        {
            Common c = new Common();
            TransIDs = trans;
            RRPID = c.Random(2);
            BrandID = brand;
            maDH = maDonHang;
            maKH = maKhachhang;
            ngayDH = ngay;
            tien = tongTien;
        }
        public int getMaDH()
        {
            return maDH;
        }
        public string OIToString()
        {
            return TransIDs + ":" + RRPID + ":" + BrandID + ":" + maDH + ":" + maKH + ":" + ngayDH + ":" + tien;
        }
    }
     
}
