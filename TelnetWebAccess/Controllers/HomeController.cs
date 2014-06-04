using System.Web.Mvc;

namespace TelnetWebAccess.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Telnet()
        {
            return View();
        }
    }
}