using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

namespace WebsiteBanSach.Controllers
{
    public class GioHangController : Controller
    {
        // GET: GioHang
        static DonHang dh;
        static double tongTien=0;
        static int tongSoLuong=0;
        DataClasses1DataContext data = new DataClasses1DataContext();
        #region Giỏ hàng
        //Lấy giỏ hàng 
        public List<GioHang> LayGioHang()
        {
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang == null)
            {
                //Nếu giỏ hàng chưa tồn tại thì mình tiến hành khởi tao list giỏ hàng (sessionGioHang)
                lstGioHang = new List<GioHang>();
                Session["GioHang"] = lstGioHang;
                
            }
            return lstGioHang;
        }
        //Thêm giỏ hàng
        public ActionResult ThemGioHang(int iMaSach, string strURL)
        {
            if (dh != null)
            {
                return Redirect(strURL);
            }
            //Lấy ra session giỏ hàng
            List<GioHang> lstGioHang = LayGioHang();
            //Kiểm tra sách này đã tồn tại trong session[giohang] chưa
            GioHang sanpham = lstGioHang.Find(n => n.iMaSach == iMaSach);
            if (sanpham == null)
            {
                sanpham = new GioHang(iMaSach);
                //Add sản phẩm mới thêm vào list
                lstGioHang.Add(sanpham);
                return Redirect(strURL);
            }
            else
            {
                sanpham.iSoLuong++;
                return Redirect(strURL);
            }
        }
        //Tính tổng số lượng
        private int TongSoLuong()
        {
            int iTongSoLuong = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                iTongSoLuong = lstGioHang.Sum(n => n.iSoLuong);
            }
            return iTongSoLuong;
        }
        //Tính tổng thành tiền
        private double TongTien()
        {
            double dTongTien = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                dTongTien = lstGioHang.Sum(n => n.dThanhTien);
            }
            return dTongTien;
        }
        //Xây dựng trang giỏ hàng
        public ActionResult GioHang()
        {
            if (dh != null)
            {
                return RedirectToAction("ThanhToan", "GioHang");
            }
            List<GioHang> lstGioHang = LayGioHang();
            if (lstGioHang.Count == 0)
            {
                RedirectToAction("Index", "Home");
            }
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            return View(lstGioHang);
        }
        //thông tin giỏ hàng trên menu
        public ActionResult GioHangPartial()
        {
            ViewBag.TongSoLuong = TongSoLuong();
            return PartialView();
        }
        //xóa 1 sản phẩm trong giỏ hàng
        public ActionResult DeleteGioHang(int id)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanpham = lstGioHang.SingleOrDefault(n => n.iMaSach == id);
            if (sanpham != null)
            {
                lstGioHang.RemoveAll(n => n.iMaSach == id);
                return RedirectToAction("GioHang");
            }
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("GioHang");
        }
        //cập nhật giỏ hàng
        public ActionResult UpdateGioHang(int id, FormCollection collection)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanpham = lstGioHang.SingleOrDefault(n => n.iMaSach == id);
            if (sanpham != null)
            {
                sanpham.iSoLuong = int.Parse(collection["SoLuong"].ToString());
            }
            return RedirectToAction("GioHang");
        }
        //xóa tất cả giỏ hàng
        public ActionResult DeleteAllGioHang()
        {
            List<GioHang> lstGioHang = LayGioHang();
            lstGioHang.Clear();
            Session["GioHang"] = null;
            dh = null;
            return RedirectToAction("Index", "Home");
        }
        #endregion
        #region Đặt hàng
        [HttpGet]
        public ActionResult DatHang()
        {
            List<GioHang> lstGioHang = LayGioHang();
            KhachHang kh = (KhachHang)Session["TaiKhoan"];
            if(Session["TaiKhoan"]==null || kh.TaiKhoan.ToString() == "")
            {
                return RedirectToAction("DangNhap", "NguoiDung");
            }
            else if (lstGioHang.Count==0)
            {
                return RedirectToAction("Index", "Home");
            }
            
            ViewBag.TongSoLuong = TongSoLuong();
            tongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            tongTien = TongTien();
            return View(lstGioHang);
        }
        [HttpPost]
        public ActionResult DatHang(FormCollection collection)
        {            
            KhachHang kh = (KhachHang)Session["TaiKhoan"];
            List<GioHang> lstGioHang = LayGioHang();
            dh = new DonHang();
            dh.MaKH = kh.MaKH;
            dh.NgayDat = DateTime.Now;
            dh.NgayGiao = DateTime.Now;
            dh.TinhTrangGiaoHang = 0;
            dh.DaThanhToan = 0;
            data.DonHangs.InsertOnSubmit(dh);
            data.SubmitChanges();
            //thêm chi tiết đơn hàng
            foreach(var item in lstGioHang)
            {
                ChiTietDonHang ctdh = new ChiTietDonHang();
                ctdh.MaDonHang = dh.MaDonHang;
                ctdh.MaSach = item.iMaSach;
                ctdh.SoLuong = item.iSoLuong;
                ctdh.DonGia = (decimal)item.dDonGia;
                data.ChiTietDonHangs.InsertOnSubmit(ctdh);
            }
            data.SubmitChanges();            
            string customer = dh.MaKH + "-" + kh.TaiKhoan + "-" + dh.MaDonHang + "-" + tongSoLuong + "-" + tongTien;
            Session["GioHang"] = null;
            dh = null;
            
            return Redirect($"https://www.thanhtoan.baongoc.com:1223/Home/Index?oi={customer}");
        }
        [HttpGet]
        public ActionResult ThanhToan(string thongbao)
        {
            KhachHang kh = (KhachHang)Session["TaiKhoan"];
            string customer = dh.MaKH + "-"+ kh.TaiKhoan + "-" + dh.MaDonHang + "-" + tongSoLuong + "-" + tongTien;
            ViewBag.customerInfo = customer;
            return View();
        }
        [HttpPost]
        public ActionResult ThanhToan(FormCollection collection)
        {

            Session["GioHang"] = null;
            dh = null;
            return RedirectToAction("Index", "Home");
            
        }
        #endregion
        #region Quản lý đơn hàng
        public ActionResult QuanLyDonHang(string txtTimKiem)
        {
            ViewBag.tukhoa = txtTimKiem;
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            ViewBag.KhachHang = data.KhachHangs.ToList();
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.DonHangs.OrderBy(n => n.DaThanhToan));
            }
            var lstKQTK = data.DonHangs.Where(n => n.MaDonHang.ToString().Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n => n.DaThanhToan));
        }
        public ActionResult DetailDonHang(int id)
        {
            var ct = data.ChiTietDonHangs.Where(n => n.MaDonHang == id).ToList();
            ViewBag.MaDonhang = id;
            ViewBag.TenSach = data.Saches.ToList();
            return View(ct);
        }
        public ActionResult DeleteDonHang(int id)
        {
            var deletelinq = (from s in data.DonHangs where s.MaDonHang == id select s).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.DonHangs.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLyDonHang");
        }
        public ActionResult EditDonHang(int id)
        {
            var donhang = data.DonHangs.SingleOrDefault(n => n.MaDonHang == id);
            ViewBag.ngaydat = donhang.NgayDat.Value.Year + "-" + Convert.ToDecimal(donhang.NgayDat.Value.Month).ToString("00") + "-" + Convert.ToDecimal(donhang.NgayDat.Value.Day).ToString("00");
            ViewBag.ngaygiao = donhang.NgayGiao.Value.Year + "-" + Convert.ToDecimal(donhang.NgayGiao.Value.Month).ToString("00") + "-" + Convert.ToDecimal(donhang.NgayGiao.Value.Day).ToString("00");
            return View(donhang);
        }
        [HttpPost]
        public ActionResult EditDonHang(FormCollection collection, int id)
        {
            var dh = data.DonHangs.SingleOrDefault(n => n.MaDonHang == id);
            UpdateModel(dh);
            data.SubmitChanges();
            return RedirectToAction("QuanLyDonHang");
        }
        public ActionResult HuyDonHang(int maDH)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "localhost";
            builder.UserID = "sa";
            builder.Password = "123456";
            builder.InitialCatalog = "QuanLyBanSach";
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                string sql;
                StringBuilder sb = new StringBuilder();

                sb.Append("DELETE FROM ChiTietDonHang WHERE MaDonHang = @maDH;");
                sql = sb.ToString();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@maDH", maDH);
                    int rowsAffected = command.ExecuteNonQuery();
                }
                sb.Clear();
                sb.Append("DELETE FROM DonHang WHERE MaDonHang = @maDH;");
                sql = sb.ToString();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@maDH", maDH);
                    int rowsAffected = command.ExecuteNonQuery();
                }
                connection.Close();
            }
            return View();
        }
        #endregion
        private static readonly string[] VietNamChar = new string[]
    {
        "aAeEoOuUiIdDyY",
        "áàạảãâấầậẩẫăắằặẳẵ",
        "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
        "éèẹẻẽêếềệểễ",
        "ÉÈẸẺẼÊẾỀỆỂỄ",
        "óòọỏõôốồộổỗơớờợởỡ",
        "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
        "úùụủũưứừựửữ",
        "ÚÙỤỦŨƯỨỪỰỬỮ",
        "íìịỉĩ",
        "ÍÌỊỈĨ",
        "đ",
        "Đ",
        "ýỳỵỷỹ",
        "ÝỲỴỶỸ"
    };
        public static string LocDau(string str)
        {
            //Thay thế và lọc dấu từng char      
            for (int i = 1; i < VietNamChar.Length; i++)
            {
                for (int j = 0; j < VietNamChar[i].Length; j++)
                    str = str.Replace(VietNamChar[i][j], VietNamChar[0][i - 1]);
            }
            return str;
        }

    }
}