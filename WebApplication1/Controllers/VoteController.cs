using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers
{
    public class VoteController : Controller
    {
        public IActionResult National()
        {
            var fodselsnr = HttpContext.Session.GetString("Fodselsnr");
            var navn = HttpContext.Session.GetString("UserFullName");

            // Hvis ikke logget inn, send til login
            if (string.IsNullOrEmpty(fodselsnr))
            {
                return RedirectToAction("Login", "Account");
            }

            // Statisk liste over partier (kan erstattes med DB senere)
            var parties = new List<string>
            {
                "Arbeiderpartiet",
                "Høyre",
                "Senterpartiet",
                "Fremskrittspartiet",
                "Sosialistisk Venstreparti",
                "Venstre",
                "Kristelig Folkeparti",
                "Rødt",
                "Miljøpartiet De Grønne"
            };

            ViewBag.Parties = parties;
            ViewBag.UserFullName = navn;

            return View();
        }

        [HttpPost]
        public IActionResult SubmitVote(string party)
        {
            // TODO: lagre stemme i DB
            TempData["VoteMessage"] = $"Du har stemt på {party}. Takk for din stemme!";
            return RedirectToAction("National");
        }
    }
}