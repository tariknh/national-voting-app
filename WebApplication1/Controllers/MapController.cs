using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Data;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    public class MapController : Controller
    {
        // Configure the release time year, month, day, houer, minute, second, UTC
        private static readonly DateTime ReleaseTime = new DateTime(2029, 9, 01, 21, 0, 0, DateTimeKind.Utc);
        private readonly ApplicationDbContext _context;

        // Party configuration moved from JavaScript
        private static readonly Dictionary<string, string> PartyColors = new Dictionary<string, string>
        {
            {"ap", "#e63946"},
            {"hoyre", "#219ebc"},
            {"sp", "#06a94d"},
            {"frp", "#023e8a"},
            {"sv", "#d62828"},
            {"rodt", "#9d0208"},
            {"venstre", "#38b000"},
            {"krf", "#ffba08"},
            {"mdg", "#2d6a4f"},
            {"inp", "#99968d"},
            {"pdk", "#99968d"},
            {"demokratene", "#99968d"},
            {"liberalistene", "#99968d"},
            {"pensjonistpartiet", "#99968d"},
            {"kystpartiet", "#99968d"},
            {"alliansen", "#99968d"},
            {"nkp", "#99968d"},
            {"piratpartiet", "#99968d"},
            {"helsepartiet", "#99968d"},
            {"folkestyret", "#99968d"},
            {"norsk_republikansk_allianse", "#99968d"},
            {"verdipartiet", "#99968d"},
            {"partiet_sentrum", "#99968d"}
        };

        private static readonly Dictionary<string, string> PartyNames = new Dictionary<string, string>
        {
            {"ap", "Arbeiderpartiet"},
            {"hoyre", "Høyre"},
            {"sp", "Senterpartiet"},
            {"frp", "Fremskrittspartiet"},
            {"sv", "SV"},
            {"rodt", "Rødt"},
            {"venstre", "Venstre"},
            {"krf", "KrF"},
            {"mdg", "MDG"},
            {"inp", "INP"},
            {"pdk", "PDK"},
            {"demokratene", "Demokratene"},
            {"liberalistene", "Liberalistene"},
            {"pensjonistpartiet", "Pensjonistpartiet"},
            {"kystpartiet", "Kystpartiet"},
            {"alliansen", "Alliansen"},
            {"nkp", "NKP"},
            {"piratpartiet", "Piratpartiet"},
            {"helsepartiet", "Helsepartiet"},
            {"folkestyret", "Folkestyret"},
            {"norsk_republikansk_allianse", "Norsk Rep. Allianse"},
            {"verdipartiet", "Verdipartiet"},
            {"partiet_sentrum", "Partiet Sentrum"}
        };

        // Main parties for the legend
        private static readonly List<string> MainParties = new List<string>
        {
            "ap", "hoyre", "sp", "frp", "sv", "rodt", "venstre", "krf", "mdg"
        };

        public MapController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var isDataAvailable = DateTime.UtcNow >= ReleaseTime;
            var currentTime = DateTime.UtcNow;
            
            ViewData["Title"] = "Valgkart Norge";
            ViewData["IsMapAvailable"] = isDataAvailable;
            ViewData["ReleaseTime"] = ReleaseTime.ToString("o"); // ISO 8601 format
            ViewData["CurrentTime"] = currentTime.ToString("o");
            ViewData["PartyColors"] = PartyColors;
            ViewData["PartyNames"] = PartyNames;
            ViewData["MainParties"] = MainParties;
            
            // Log for debugging
            Console.WriteLine($"Current UTC time: {currentTime}");
            Console.WriteLine($"Release time: {ReleaseTime}");
            Console.WriteLine($"Data available: {isDataAvailable}");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetVotingData()
        {
            // Check if data should be released
            if (DateTime.UtcNow < ReleaseTime)
            {
                // Return 403 Forbidden with information about when data will be available
                Response.Headers.Add("X-Release-Time", ReleaseTime.ToString("o"));
                return StatusCode(403, new { 
                    error = "Data not yet available",
                    releaseTime = ReleaseTime.ToString("o"),
                    currentTime = DateTime.UtcNow.ToString("o")
                });
            }
            
            try
            {
                var votingData = await _context.Stemmers.ToListAsync();
                return Json(votingData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching voting data: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProcessedVotingData()
        {
            // Protect Data Access
            if (DateTime.UtcNow < ReleaseTime)
            {
                Response.Headers.Add("X-Release-Time", ReleaseTime.ToString("o"));
                return StatusCode(403, new { 
                    error = "Data not yet available",
                    releaseTime = ReleaseTime.ToString("o"),
                    currentTime = DateTime.UtcNow.ToString("o")
                });
            }
            
            try
            {
                var votingData = await _context.Stemmers.ToListAsync();
                
                // Process the data server-side
                var processedData = votingData.Select(municipality => 
                {
                    var partyVotes = GetPartyVotes(municipality);
                    var leadingParty = GetLeadingParty(partyVotes);
                    var totalVotes = partyVotes.Sum(p => p.Votes);

                    return new
                    {
                        Kommune = municipality.Kommune,
                        NormalizedName = NormalizeName(municipality.Kommune),
                        LeadingParty = leadingParty,
                        TotalVotes = totalVotes,
                        PartyResults = partyVotes,
                        RawData = municipality // Include raw data if needed
                    };
                }).ToList();

                return Json(new
                {
                    Data = processedData,
                    Config = new
                    {
                        PartyColors = PartyColors,
                        PartyNames = PartyNames,
                        MainParties = MainParties
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing voting data: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet]
        public IActionResult GetPartyConfiguration()
        {
            return Json(new
            {
                PartyColors = PartyColors,
                PartyNames = PartyNames,
                MainParties = MainParties
            });
        }

        [HttpGet]
        public IActionResult GetDataStatus()
        {
            var isAvailable = DateTime.UtcNow >= ReleaseTime;
            return Json(new
            {
                isAvailable = isAvailable,
                releaseTime = ReleaseTime.ToString("o"),
                currentTime = DateTime.UtcNow.ToString("o"),
                secondsUntilRelease = isAvailable ? 0 : (int)(ReleaseTime - DateTime.UtcNow).TotalSeconds
            });
        }

        // Helper methods moved from JavaScript
        private string NormalizeName(string name)
        {
            name = name.ToLower()
                .Trim()
                .Replace(" kommune", "")
                .Replace("  ", " ");
            return name;
        }

        private List<PartyVoteResult> GetPartyVotes(dynamic municipalityData)
        {
            var results = new List<PartyVoteResult>();

            foreach (var party in PartyColors.Keys)
            {
                var votes = GetPropertyValue(municipalityData, party);
                if (votes > 0)
                {
                    results.Add(new PartyVoteResult
                    {
                        Party = party,
                        Name = PartyNames[party],
                        Votes = votes,
                        Color = PartyColors[party]
                    });
                }
            }

            return results.OrderByDescending(p => p.Votes).ToList();
        }

        private PartyVoteResult GetLeadingParty(List<PartyVoteResult> partyVotes)
        {
            return partyVotes.FirstOrDefault() ?? new PartyVoteResult
            {
                Party = null,
                Name = "Ingen stemmer",
                Votes = 0,
                Color = "#cccccc"
            };
        }

        private int GetPropertyValue(dynamic obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    return value != null ? Convert.ToInt32(value) : 0;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // Helper class for party vote results
        public class PartyVoteResult
        {
            public string Party { get; set; }
            public string Name { get; set; }
            public int Votes { get; set; }
            public string Color { get; set; }
        }
    }
}