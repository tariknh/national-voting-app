using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;
using System.Collections.Generic;

namespace WebApplication1.Controllers
{
    public class VoteController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Konstruktør-injeksjon for DbContext
        public VoteController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult National()
        {
            var fodselsnr = HttpContext.Session.GetString("Fodselsnr");
            var navn = HttpContext.Session.GetString("UserFullName");

            // Hvis ikke logget inn, send til login
            if (string.IsNullOrEmpty(fodselsnr))
            {
                return RedirectToAction("Login", "Account");
            }

            // Statisk liste over partier
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
                "Miljøpartiet De Grønne",
                "Innsamslingspartiet", // inp
                "Partiet De Kristne", // pdk
                "Demokratene",
                "Liberalistene",
                "Pensjonistpartiet",
                "Kystpartiet",
                "Alliansen",
                "Norges Kommunistiske Parti", // nkp
                "Piratpartiet",
                "Helsepartiet",
                "Folkestyret",
                "Norsk Republikanse Allianse",
                "Verdipartiet",
                "Partiet Sentrum"
            };

            ViewBag.Parties = parties;
            ViewBag.UserFullName = navn;

            return View();
        }

        [HttpPost]
        public IActionResult SubmitVote(string party)
        {
            var fodselsnr = HttpContext.Session.GetString("Fodselsnr");
            if (string.IsNullOrEmpty(fodselsnr))
            {
                return RedirectToAction("Login", "Account");
            }

            // Hent brukeren fra databasen
            var user = _context.Users.FirstOrDefault(u => u.Fodselsnr == fodselsnr);
            if (user == null)
            {
                TempData["VoteMessage"] = "Bruker ikke funnet!";
                return RedirectToAction("National");
            }

            // Sjekk om brukeren allerede har stemt
            if (user.HasVoted == true)
            {
                TempData["VoteMessage"] = "Du har allerede avgitt din stemme!";
                return RedirectToAction("National");
            }

            // Hent kommunen fra session
            var kommune = HttpContext.Session.GetString("Kommune");
            if (string.IsNullOrEmpty(kommune))
            {
                kommune = "Ukjent"; // fallback
            }

            // Finn rad for kommunen
            var stemme = _context.Stemmers.FirstOrDefault(s => s.Kommune == kommune);
            if (stemme == null)
            {
                stemme = new Stemmer { Kommune = kommune };
                _context.Stemmers.Add(stemme);
            }

            // Oppdater riktig parti
            switch (party.ToLower())
            {
                case "arbeiderpartiet": stemme.Ap++; break;
                case "høyre": stemme.Hoyre++; break;
                case "senterpartiet": stemme.Sp++; break;
                case "fremskrittspartiet": stemme.Frp++; break;
                case "sosialistisk venstreparti": stemme.Sv++; break;
                case "venstre": stemme.Venstre++; break;
                case "kristelig folkeparti": stemme.Krf++; break;
                case "rødt": stemme.Rodt++; break;
                case "miljøpartiet de grønne": stemme.Mdg++; break;
                case "innsamslingspartiet": stemme.Inp++; break;
                case "partiet de kristne": stemme.Pdk++; break;
                case "demokratene": stemme.Demokratene++; break;
                case "liberalistene": stemme.Liberalistene++; break;
                case "pensjonistpartiet": stemme.Pensjonistpartiet++; break;
                case "kystpartiet": stemme.Kystpartiet++; break;
                case "alliansen": stemme.Alliansen++; break;
                case "norges kommunistiske parti": stemme.Nkp++; break;
                case "piratpartiet": stemme.Piratpartiet++; break;
                case "helsepartiet": stemme.Helsepartiet++; break;
                case "folkestyret": stemme.Folkestyret++; break;
                case "norsk republikanse allianse": stemme.NorskRepublikanskAllianse++; break;
                case "verdipartiet": stemme.Verdipartiet++; break;
                case "partiet sentrum": stemme.PartietSentrum++; break;
               
                default:
                    TempData["VoteMessage"] = "Ugyldig parti valgt.";
                    return RedirectToAction("National");
            }

            // Sett HasVoted på brukeren
            user.HasVoted = true;

            // Lagre endringer i databasen
            _context.SaveChanges();

            TempData["VoteMessage"] = $"Stemmen din for {party} i {kommune} er registrert!";
            return RedirectToAction("National");
        }
    }
}


