// Controllers/VoteController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebApplication1.Data;
using WebApplication1.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace WebApplication1.Controllers
{
    public class VoteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VotingTokenService _tokenService;

        public VoteController(
            ApplicationDbContext context,
            VotingTokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public IActionResult National()
        {
            var fodselsnr = HttpContext.Session.GetString("Fodselsnr");
            var navn = HttpContext.Session.GetString("UserFullName");

            if (string.IsNullOrEmpty(fodselsnr))
            {
                return RedirectToAction("Login", "Account");
            }

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
                "Innsamslingspartiet",
                "Partiet De Kristne",
                "Demokratene",
                "Liberalistene",
                "Pensjonistpartiet",
                "Kystpartiet",
                "Alliansen",
                "Norges Kommunistiske Parti",
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
        public async Task<IActionResult> SubmitVote(string party)
        {
            var fodselsnr = HttpContext.Session.GetString("Fodselsnr");
            if (string.IsNullOrEmpty(fodselsnr))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 1. Konverter parti til int
                int partiInt = PartyMapper.GetPartyId(party);
                if (partiInt == -1)
                {
                    TempData["VoteMessage"] = "Ugyldig parti valgt.";
                    return RedirectToAction("National");
                }

                // 2. Hent kommune
                var kommune = HttpContext.Session.GetString("Kommune") ?? "Ukjent";

                // 3. Generer full token (markerer HasVoted=true)
                string fullToken = await _tokenService.GenerateVotingToken(fodselsnr);

                // 4. Lagre stemme MED full token
                await _tokenService.StoreVote(fullToken, partiInt, kommune);

                TempData["VoteMessage"] = $"Stemmen din for {party} er registrert!";
                return RedirectToAction("National");
            }
            catch (InvalidOperationException ex)
            {
                TempData["VoteMessage"] = ex.Message;
                return RedirectToAction("National");
            }
            catch (Exception ex)
            {
                TempData["VoteMessage"] = "En feil oppstod ved stemmeregistrering.";
                Console.WriteLine($"Voting error: {ex.Message}");
                return RedirectToAction("National");
            }
        }

        /// <summary>
        /// Admin-funksjon: Verifiser alle stemmer etter valget
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerifyVotes()
        {
            // TODO: Legg til admin-autentisering her
            
            var result = await _tokenService.VerifyAllVotes();
            
            ViewBag.Result = result;
            return View();
        }
    }
}