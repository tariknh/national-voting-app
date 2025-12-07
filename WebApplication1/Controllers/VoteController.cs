using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
	[Authorize]
	public class VoteController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly VotingTokenService _tokenService;

		private static readonly IReadOnlyList<string> Parties = new List<string>
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

		public VoteController(ApplicationDbContext context, VotingTokenService tokenService)
		{
			_context = context;
			_tokenService = tokenService;
		}

		public async Task<IActionResult> National()
		{
			var user = await GetCurrentUserAsync();
			if (user == null)
			{
				TempData["VoteMessage"] = "Vi klarte ikke å finne brukeren. Logg inn på nytt.";
				return RedirectToAction("Index", "Home");
			}

			var model = new VoteViewModel
			{
				FullName = BuildFullName(user) ?? User.Identity?.Name,
				Kommune = user.Kommune ?? "Ukjent",
				HasVoted = user.HasVoted ?? false,
				Parties = Parties,
				StatusMessage = TempData["VoteMessage"] as string
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SubmitVote(string party)
		{
			if (string.IsNullOrWhiteSpace(party))
			{
				TempData["VoteMessage"] = "Velg et parti før du sender inn.";
				return RedirectToAction(nameof(National));
			}

			var user = await GetCurrentUserAsync();
			if (user == null)
			{
				TempData["VoteMessage"] = "Økten din er utløpt. Logg inn igjen.";
				return RedirectToAction("Login", "Home");
			}

			if (user.HasVoted == true)
			{
				TempData["VoteMessage"] = "Du har allerede avgitt din stemme.";
				return RedirectToAction(nameof(National));
			}

			var kommune = string.IsNullOrWhiteSpace(user.Kommune) ? "Ukjent" : user.Kommune;
			var stemme = await _context.Stemmers.FirstOrDefaultAsync(s => s.Kommune == kommune);
			if (stemme == null)
			{
				stemme = new Stemmer { Kommune = kommune };
				_context.Stemmers.Add(stemme);
			}

			if (!IncrementPartyCounter(stemme, party))
			{
				TempData["VoteMessage"] = "Ugyldig parti valgt.";
				return RedirectToAction(nameof(National));
			}

			// ========== NY FUNKSJONALITET: Kryptert token ==========
			try
			{
				// Hent BankIdUuid
				var bankIdUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (!string.IsNullOrEmpty(bankIdUuid))
				{
					// Konverter parti til int
					int partiInt = PartyMapper.GetPartyId(party);
					if (partiInt != -1)
					{
						// Generer og lagre kryptert token
						string fullToken = await _tokenService.GenerateVotingTokenByBankId(bankIdUuid);
						await _tokenService.StoreVote(fullToken, partiInt, kommune);
					}
				}
			}
			catch (Exception ex)
			{
				// Log feilen, men fortsett med normal stemmeregistrering
				Console.WriteLine($"Token generation error: {ex.Message}");
			}
			// ========== SLUTT NY FUNKSJONALITET ==========

			user.HasVoted = true;
			await _context.SaveChangesAsync();

			TempData["VoteMessage"] = $"Stemmen din for {party} i {kommune} er registrert.";
			return RedirectToAction(nameof(National));
		}

		private async Task<User?> GetCurrentUserAsync()
		{
			var bankIdUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(bankIdUuid))
			{
				return null;
			}

			return await _context.Users.FirstOrDefaultAsync(u => u.BankIdUuid == bankIdUuid);
		}

		private static string? BuildFullName(User user)
		{
			var first = user.Firstname?.Trim();
			var last = user.Lastname?.Trim();
			if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
			{
				return null;
			}

			if (string.IsNullOrEmpty(first))
			{
				return last;
			}

			if (string.IsNullOrEmpty(last))
			{
				return first;
			}

			return $"{first} {last}";
		}

		private static bool IncrementPartyCounter(Stemmer stemme, string party)
		{
			var normalized = party.Trim().ToLowerInvariant();
			switch (normalized)
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
					return false;
			}

			return true;
		}
	}
}