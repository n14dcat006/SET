using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBanSach.Models;

using System.Data;
using System.IO;


namespace WebsiteBanSach.Controllers
{
    public class HomeController : Controller
    {
        // GET: Default
        DataClasses1DataContext data = new DataClasses1DataContext();
        
        public ActionResult Index()
        {
            var all_sach = from tt in data.Saches select tt;
            var all_tacgia = from tt in data.TacGias select tt;
            ViewBag.tacgia = all_tacgia.ToList();
            ViewBag.sachmoi = all_sach.OrderByDescending(a => a.NgayCapNhat).Take(4).ToList();
            ViewBag.sachtop = all_sach.OrderBy(a => a.SoLuongTon).Take(4).ToList();
            return View(all_sach);
        }
        
    }
}