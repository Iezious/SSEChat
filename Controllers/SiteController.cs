using Microsoft.AspNetCore.Mvc;

namespace SEEChat.Controllers
{
    [Route("")]
    public class SiteController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}