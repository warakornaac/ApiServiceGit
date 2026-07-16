using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ApiService.Controllers
{
    public class GlobalEcatalogController : Controller
    {
        // GET: GlobalEcatalog
        public ActionResult Index()
        {
            return View();
        }
    }
}