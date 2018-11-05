using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

namespace WebsiteBanSach.Controllers
{
    public class NguoiDungController : Controller
    {
        // GET: NguoiDung
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult QuanLyNguoiDung(string txtTimKiem)
        {
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            ViewBag.tukhoa = txtTimKiem;
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.KhachHangs.OrderBy(n => n.TaiKhoan));
            }
            var lstKQTK = data.KhachHangs.Where(n => n.TaiKhoan.Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n => n.TaiKhoan));
        }
        //hàm xóa người dùng
        public ActionResult DeleteNguoiDung(int id)
        {
            var deletelinq = (from cd in data.KhachHangs where cd.MaKH == id select cd).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.KhachHangs.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLyNguoiDung");
        }
        //hàm sửa
        public ActionResult EditNguoiDung(int id)
        {
            var khachhang = data.KhachHangs.SingleOrDefault(n => n.MaKH == id);
            ViewBag.ngay = khachhang.NgaySinh.Value.Year + "-" + Convert.ToDecimal(khachhang.NgaySinh.Value.Month).ToString("00") + "-" + Convert.ToDecimal(khachhang.NgaySinh.Value.Day).ToString("00");
            return View(khachhang);
        }
        [HttpPost]
        public ActionResult EditNguoiDung(FormCollection collection, int id)
        {
            var khachhang = data.KhachHangs.SingleOrDefault(n => n.MaKH == id);
            var mk = collection["MatKhau"];
            var nhaplaimk = collection["NhapLaiMatKhau"];
            if (mk.CompareTo(nhaplaimk) == 0)
            {
                UpdateModel(khachhang);
                data.SubmitChanges();
                return RedirectToAction("QuanLyNguoiDung");
            }
            else
            {
                ViewBag.Thongbao = "mật khẩu và Nhập lại mật khẩu phải giống nhau";
                return this.EditNguoiDung(id);
            }
        }
        public ActionResult DangKy()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangKy(FormCollection collection, KhachHang khachhang)
        {
            var mk = collection["MatKhau"];
            var nhaplaimk = collection["NhapLaiMatKhau"];
            if (mk.CompareTo(nhaplaimk) == 0)
            {
                data.KhachHangs.InsertOnSubmit(khachhang);
                data.SubmitChanges();
                return RedirectToAction("QuanLyNguoiDung");
            }
            else
            {
                ViewBag.Thongbao = "mật khẩu và Nhập lai mật khẩu phải giống nhau";
                return this.DangKy();
            }
        }
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangNhap(FormCollection collection)
        {
            var taikhoan = collection["TaiKhoan"];
            var matkhau = collection["MatKhau"];
            KhachHang kh = data.KhachHangs.SingleOrDefault(n=> n.TaiKhoan == taikhoan && n.MatKhau == matkhau);
            if (kh != null)
            {
                Session["TaiKhoan"] = kh;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                
                ViewBag.Thongbao1 = "Tên đăng nhập hoặc mật khẩu không đúng";
                return this.View();
            }
        }
        public ActionResult DangXuat()
        {
            Session["TaiKhoan"] = null;
            return RedirectToAction("Index", "Home");
        }
    }
}