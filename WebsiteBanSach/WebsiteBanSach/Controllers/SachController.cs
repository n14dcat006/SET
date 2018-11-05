using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;
using System.IO;
using PagedList;
using PagedList.Mvc;

namespace WebsiteBanSach.Controllers
{
    public class SachController : Controller
    {
       
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult TongHopSach()
        {
            ViewBag.abc = data.Saches.OrderBy(n => n.TenSach).ToList();
            ViewBag.gia = data.Saches.OrderBy(n => n.GiaBan).ToList();
            ViewBag.moi = data.Saches.OrderBy(n => n.NgayCapNhat).ToList();
            ViewBag.tacgia = data.TacGias.ToList();
            return View();
        }
        //trang trinh bay chi tiet

        public ActionResult QuanLySach(string txtTimKiem)
        {
            ViewBag.chude = data.ChuDes.ToList();
            ViewBag.nxb = data.NhaXuatBans.ToList();
            ViewBag.tacgia = data.TacGias.ToList();
            ViewBag.tukhoa = txtTimKiem;
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.Saches.OrderBy(n => n.TenSach));
            }
            var lstKQTK = data.Saches.Where(n => n.TenSach.Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n=>n.TenSach));
        }
        //hàm create sách
        public ActionResult CreateSach()
        {
            ViewBag.MaNXB = new SelectList(data.NhaXuatBans.OrderBy(n => n.TenNXB), "MaNXB", "TenNXB");
            ViewBag.MaChuDe = new SelectList(data.ChuDes.OrderBy(n => n.TenChuDe), "MaChuDe", "TenChuDe");
            ViewBag.MaTacGia = new SelectList(data.TacGias.OrderBy(n => n.TenTacGia), "MaTacGia", "TenTacGia");
            return View();
        }
        [HttpPost]
        public ActionResult CreateSach(FormCollection collection, Sach sach, HttpPostedFileBase fileupload)
        {
            var filename = Path.GetFileName(fileupload.FileName);
            var path = Path.Combine(Server.MapPath("~/ImagesBook"), filename);
            if (System.IO.File.Exists(path))
            {
                ViewBag.Thongbao = "Hình ảnh đã tồn tại";
            }
            else
            {
                fileupload.SaveAs(path);
            }
            sach.AnhBia = "\\ImagesBook\\" + fileupload.FileName;
            data.Saches.InsertOnSubmit(sach);
            data.SubmitChanges();
            return RedirectToAction("QuanLySach");//sau khi nhập xong chuyển về trang
        }
        //hàm xóa sách
        public ActionResult DeleteSach(int id)
        {
            var deletelinq = (from s in data.Saches where s.MaSach == id select s).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.Saches.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLySach");
        }
        //ham chi tiet sach
        public ActionResult DetailSach(int id)
        {
            var sach = data.Saches.SingleOrDefault(n => n.MaSach == id);
            ViewBag.TenTacGia = data.TacGias.SingleOrDefault(n => n.MaTacGia == sach.MaTacGia).TenTacGia;
            return View(sach);
        }
        //hàm sửa
        public ActionResult EditSach(int id)
        {
            var sach = data.Saches.SingleOrDefault(n => n.MaSach == id);
            ViewBag.ngay = sach.NgayCapNhat.Value.Year+"-"+Convert.ToDecimal(sach.NgayCapNhat.Value.Month).ToString("00")+"-"+ Convert.ToDecimal(sach.NgayCapNhat.Value.Day).ToString("00"); 
            ViewBag.MaNXB = new SelectList(data.NhaXuatBans.OrderBy(n=>n.TenNXB), "MaNXB", "TenNXB",sach.MaNXB);
            ViewBag.MaChuDe = new SelectList(data.ChuDes.OrderBy(n=>n.TenChuDe), "MaChuDe", "TenChuDe",sach.MaChuDe);
            ViewBag.MaTacGia = new SelectList(data.TacGias.OrderBy(n=>n.TenTacGia), "MaTacGia", "TenTacGia",sach.MaTacGia);
            return View(sach);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSach(FormCollection collection, int id, HttpPostedFileBase fileupload)
        {
            var sach = data.Saches.SingleOrDefault(n => n.MaSach == id);
            if (fileupload != null)
            {
                var filename = Path.GetFileName(fileupload.FileName);
                var path = Path.Combine(Server.MapPath("~/ImagesBook"), filename);
                if (System.IO.File.Exists(path))
                {
                    ViewBag.Thongbao = "Hình ảnh đã tồn tại";
                }
                else
                {
                    fileupload.SaveAs(path);
                }
                sach.AnhBia = "\\ImagesBook\\" + fileupload.FileName;
            }
            UpdateModel(sach);
            data.SubmitChanges();
            return RedirectToAction("QuanLySach","Sach");
        }
    }
}