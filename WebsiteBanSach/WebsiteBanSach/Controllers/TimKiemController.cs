using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;
using PagedList;
using PagedList.Mvc;

namespace WebsiteBanSach.Controllers
{
    public class TimKiemController : Controller
    {
        // GET: TimKiem
        DataClasses1DataContext data = new DataClasses1DataContext();
        
        [HttpGet]
        public ActionResult KetQuaTimKiem(string txtTimKiem, int? page)
        {
            ViewBag.tacgia = data.TacGias.ToList();
            ViewBag.TuKhoa = txtTimKiem;
            //Phân trang
            int pageNumber = (page ?? 1);
            int pageSize = 12;
            if (string.IsNullOrEmpty(txtTimKiem))
            {
                ViewBag.ThongBao = "Không tìm thấy sản phẩm nào";
                return View(data.Saches.OrderBy(n => n.TenSach).ToPagedList(pageNumber, pageSize));
            }
            var lstKQTK = data.Saches.Where(n => n.TenSach.Contains(txtTimKiem)).ToList();
            if (lstKQTK.Count == 0)
            {
                ViewBag.ThongBao = "Không tìm thấy sản phẩm nào";
                return View(data.Saches.OrderBy(n => n.TenSach).ToPagedList(pageNumber, pageSize));
            }
            ViewBag.ThongBao = "Đã tìm thấy " + lstKQTK.Count + " kết quả!";
            return View(lstKQTK.OrderBy(n => n.TenSach).ToPagedList(pageNumber, pageSize));
        }
    }
}