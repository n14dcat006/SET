using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

namespace WebsiteBanSach.Controllers
{
    public class ChuDeController : Controller
    {
        // GET: ChuDe
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult SachTheoChuDe()
        {
            ViewBag.listSach = data.Saches.ToList();
            var chude = data.ChuDes;
            return View(chude);
        }
        //sách thuộc chủ đề
        public ActionResult SachThuocChuDe(int id)
        {
            ViewBag.chude = data.ChuDes.SingleOrDefault(n => n.MaChuDe == id);
            ViewBag.listSach = data.Saches.Where(n => n.MaChuDe == id).OrderBy(n => n.GiaBan).ToList();
            if (ViewBag.listSach.Count == 0)
            {
                ViewBag.Thongbao = "Không có sách thuộc chủ đề này";
            }
            return View();
        }
        //danh sách chủ đề
        public ActionResult QuanLyChuDe(string txtTimKiem)
        {
            ViewBag.tukhoa = txtTimKiem;
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.ChuDes.OrderBy(n => n.TenChuDe));
            }
            var lstKQTK = data.ChuDes.Where(n => n.TenChuDe.Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n => n.TenChuDe));
        }
        //hàm create chủ đề
        public ActionResult CreateChuDe()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateChuDe(FormCollection collection, ChuDe chude)
        {
            data.ChuDes.InsertOnSubmit(chude);
            data.SubmitChanges();
            return RedirectToAction("QuanLyChuDe");//sau khi nhập xong chuyển về trang
        }
        //hàm xóa chủ đề
        public ActionResult DeleteChuDe(int id)
        {
            var deletelinq = (from cd in data.ChuDes where cd.MaChuDe == id select cd).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.ChuDes.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLyChuDe");
        }
        //hàm sửa chủ đề
        public ActionResult EditChuDe(int id)
        {
            var chude = data.ChuDes.SingleOrDefault(n => n.MaChuDe == id);
            return View(chude);
        }
        [HttpPost]
        public ActionResult EditChuDe(FormCollection collection, int id)
        {
            var ten_chude = collection["TenChuDe"];
            var chude = data.ChuDes.SingleOrDefault(n => n.MaChuDe == id);
            chude.TenChuDe = ten_chude;
            UpdateModel(chude);
            data.SubmitChanges();
            return RedirectToAction("QuanLyChuDe");
        }
        //partial chủ đề
        public ActionResult ChuDePartial()
        {
            return PartialView(data.ChuDes);
        }
    }
}