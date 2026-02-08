using Microsoft.AspNetCore.Mvc;

namespace AceJobAgency.Controllers
{
    public class StatusCodeController : Controller
    {
        [HttpGet("StatusCode/{statusCode}")]
        public IActionResult Index(int statusCode)
        {
            ViewBag.StatusCode = statusCode;
            return View("StatusCodeError");
        }
    }
}
