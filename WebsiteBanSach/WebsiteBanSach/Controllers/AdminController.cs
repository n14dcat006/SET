using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

namespace WebsiteBanSach.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult QuanLyAdmin(string txtTimKiem)
        {
            ViewBag.tukhoa = txtTimKiem;
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.Admins.OrderBy(n => n.TaiKhoanAdmin));
            }
            var lstKQTK = data.Admins.Where(n => n.TaiKhoanAdmin.Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n => n.TaiKhoanAdmin));
        }
        //hàm tạo admin
        public ActionResult CreateAdmin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateAdmin(FormCollection collection, Admin admin)
        {
            var mk = collection["MatKhau"];
            var nhaplaimk = collection["NhapLaiMatKhau"];
            if (mk.CompareTo(nhaplaimk) == 0)
            {
                data.Admins.InsertOnSubmit(admin);
                data.SubmitChanges();
                return RedirectToAction("QuanLyAdmin");//sau khi nhập xong chuyển về trang
            }
            else
            {
                ViewBag.Thongbao = "mật khẩu và Nhập lại mật khẩu phải giống nhau";
                return this.CreateAdmin();
            }
        }
        //hàm xóa Admin
        public ActionResult DeleteAdmin(int id)
        {
            var deletelinq = (from cd in data.Admins where cd.MaAdmin == id select cd).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.Admins.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLyAdmin");
        }
        //hàm sửa Admin
        public ActionResult EditAdmin(int id)
        {
            var admin = data.Admins.SingleOrDefault(n => n.MaAdmin == id);
            return View(admin);
        }
        [HttpPost]
        public ActionResult EditAdmin(FormCollection collection, int id)
        {
            var admin = data.Admins.SingleOrDefault(n => n.MaAdmin == id);
            var mk = collection["MatKhau"];
            var nhaplaimk = collection["NhapLaiMatKhau"];
            if (mk.CompareTo(nhaplaimk) == 0)
            {
                UpdateModel(admin);
                data.SubmitChanges();
                return RedirectToAction("QuanLyAdmin");
            }
            else
            {
                ViewBag.Thongbao = "mật khẩu và Nhập lại mật khẩu phải giống nhau";
                return this.EditAdmin(id);
            }
        }
        public ActionResult DangNhapAdmin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangNhapAdmin(FormCollection collection)
        {
            var taikhoan = collection["TaiKhoanAdmin"];
            var matkhau = collection["MatKhau"];
            Admin ad = data.Admins.SingleOrDefault(n => n.TaiKhoanAdmin == taikhoan && n.MatKhau == matkhau);
            if (ad != null)
            {
                ViewBag.ThongBao = "Chúc mừng đăng nhập thành công";
                Session["TenAdmin"] = ad.TaiKhoanAdmin;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Thongbao1 = "Tên đăng nhập hoặc mật khẩu không đúng";
                return this.View();
            }
        }
        public ActionResult DangXuatAdmin()
        {
            Session["TenAdmin"] = null;
            return RedirectToAction("Index", "Home");
        }
    }
}