using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

namespace WebsiteBanSach.Controllers
{
    public class NhaXuatBanController : Controller
    {
        // GET: NhaXuatBan
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult SachTheoNXB()
        {
            ViewBag.listSach = data.Saches.ToList();
            var nxb = data.NhaXuatBans;
            return View(nxb);
        }
        //sách thuộc nhà xuất bản
        public ActionResult SachThuocNXB(int id)
        {
            ViewBag.nxb = data.NhaXuatBans.SingleOrDefault(n => n.MaNXB == id);
            ViewBag.listSach = data.Saches.Where(n => n.MaNXB == id).OrderBy(n => n.GiaBan).ToList();
            if (ViewBag.listSach.Count == 0)
            {
                ViewBag.Thongbao = "Không có sách thuộc nhà xuất bản này";
            }
            return View();
        }
        //danh sách nhà xuất bản
        public ActionResult QuanLyNXB(string txtTimKiem)
        {
            ViewBag.tukhoa = txtTimKiem;
            if (TempData["Thongbao"] != null)
            {
                //Đưa nội dung trong tempdata vào viewbag dưới dạng chuỗi
                ViewBag.Thongbao = TempData["Thongbao"].ToString();
            }
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                return View(data.NhaXuatBans.OrderBy(n => n.TenNXB));
            }
            var lstKQTK = data.NhaXuatBans.Where(n => n.TenNXB.Contains(txtTimKiem)).ToList();
            return View(lstKQTK.OrderBy(n => n.TenNXB));
        }

        //hàm create nhà xuất bản
        public ActionResult CreateNXB()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CreateNXB(FormCollection collection, NhaXuatBan nxb)
        {
            data.NhaXuatBans.InsertOnSubmit(nxb);
            data.SubmitChanges();
            return RedirectToAction("QuanLyNXB");
        }
        //hàm xóa nhà xuất bản
        public ActionResult DeleteNXB(int id)
        {
            var deletelinq = (from cd in data.NhaXuatBans where cd.MaNXB == id select cd).SingleOrDefault();
            if (deletelinq == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            data.NhaXuatBans.DeleteOnSubmit(deletelinq);
            data.SubmitChanges();
            TempData["Thongbao"] = "Đã xóa thành công";
            return RedirectToAction("QuanLyNXB");
        }
        //hàm sửa nhà xuất bản
        public ActionResult EditNXB(int id)
        {
            var nxb = data.NhaXuatBans.SingleOrDefault(n => n.MaNXB == id);
            return View(nxb);
        }
        [HttpPost]
        public ActionResult EditNXB(FormCollection collection, int id)
        {
            var ten_nxb = collection["TenNXB"];
            var nxb = data.NhaXuatBans.SingleOrDefault(n => n.MaNXB == id);
            nxb.TenNXB = ten_nxb;
            UpdateModel(nxb);
            data.SubmitChanges();
            return RedirectToAction("QuanLyNXB");

        }
        //nhà xuất ban partial
        public ActionResult NXBPartial()
        {
            return PartialView(data.NhaXuatBans);
        }
    }
}