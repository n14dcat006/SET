using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

namespace WebsiteBanSach.Controllers
{
    public class TacGiaController : Controller
    {
        // GET: TacGia
        DataClasses1DataContext data = new DataClasses1DataContext();
        //sách thuộc tác giả
        public ActionResult SachThuocTacGia(int id)
        {
            ViewBag.tacgia = data.TacGias.SingleOrDefault(n => n.MaTacGia == id);
            ViewBag.listSach = data.Saches.Where(n => n.MaTacGia == id).OrderBy(n => n.GiaBan).ToList();
            if (ViewBag.listSach.Count == 0)
            {
                ViewBag.Thongbao = "Không có sách thuộc tác giả này";
            }
            return View();
        }
        //danh sách tác giả
        public ActionResult QuanLyTacGia(string txtTimKiem)
        {
            ViewBag.tukhoa = txtTimKiem;
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.TacGias.OrderBy(n => n.TenTacGia));
            }
            var lstKQTK = data.TacGias.Where(n => n.TenTacGia.Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n => n.TenTacGia));
        }
        //hàm create tác giả
        public ActionResult CreateTacGia()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateTacGia(FormCollection collection, TacGia tacgia)
        {
            data.TacGias.InsertOnSubmit(tacgia);
            data.SubmitChanges();
            return RedirectToAction("QuanLyTacGia");//sau khi nhập xong chuyển về trang
        }
        //hàm xóa tác giả
        public ActionResult DeleteTacGia(int id)
        {
            var deletelinq = (from cd in data.TacGias where cd.MaTacGia == id select cd).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.TacGias.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLyTacGia");
        }
        //hàm sửa tác giả
        public ActionResult EditTacGia(int id)
        {
            var tacgia = data.TacGias.SingleOrDefault(n => n.MaTacGia == id);
            return View(tacgia);
        }
        [HttpPost]
        public ActionResult EditTacGia(FormCollection collection, int id)
        {
            var ten_tacgia = collection["TenTacGia"];
            var tacgia = data.TacGias.SingleOrDefault(n => n.MaTacGia == id);
            if (string.IsNullOrEmpty(ten_tacgia))
            {
                ViewBag.Thongbao = "tên tác giả không được để trống";
            }
            else
            {
                tacgia.TenTacGia = ten_tacgia;
                UpdateModel(tacgia);
                data.SubmitChanges();
                return RedirectToAction("QuanLyTacGia");
            }
            return this.EditTacGia(id);

        }
    }
}