// Services/VotingTokenService.cs
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Services
{
    public class VotingTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _secretKey;

        public VotingTokenService(ApplicationDbContext context)
        {
            _context = context;
            
            _secretKey = Environment.GetEnvironmentVariable("VOTING_SECRET_KEY");
            
            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException(
                    "VOTING_SECRET_KEY ikke funnet i environment variables!"
                );
            }
        }

        /// <summary>
        /// Genererer voting token
        /// </summary>
        public async Task<string> GenerateVotingToken(string fodselsnr)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Fodselsnr == fodselsnr);
            
            if (user == null)
            {
                throw new InvalidOperationException("Bruker ikke funnet");
            }

            if (user.HasVoted == true)
            {
                throw new InvalidOperationException("Already voted");
            }

            // Generer tilfeldig voteId
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            string voteId = Convert.ToHexString(randomBytes).ToLower();

            // Generer HMAC signatur
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(voteId));
                string signature = Convert.ToHexString(hashBytes).ToLower();

                // Kombiner til full token
                string fullToken = $"{voteId}.{signature}";

                // Marker bruker som stemt
                user.HasVoted = true;
                await _context.SaveChangesAsync();

                return fullToken;
            }
        }

        /// <summary>
        /// Lagrer stemme MED full token (for senere verifisering)
        /// </summary>
        public async Task StoreVote(string fullToken, int partiInt, string kommune)
        {
            // Sjekk at tokenen ikke er brukt før
            var tokenExists = await _context.Vertifikasjons
                .AnyAsync(v => v.StemmeToken == fullToken);

            if (tokenExists)
            {
                throw new InvalidOperationException("Token already used");
            }

            // Lagre stemme MED full token (voteId + signature)
            var vertifikasjon = new Vertifikasjon
            {
                StemmeToken = fullToken,  // HELE tokenen: "voteId.signature"
                Parti = partiInt,
                Kommune = kommune
            };

            _context.Vertifikasjons.Add(vertifikasjon);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifiser EN stemme (etter valget)
        /// </summary>
        public bool VerifySingleVote(string fullToken)
        {
            var parts = fullToken.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            string voteId = parts[0];
            string providedSignature = parts[1];

            // Gjenskape signatur med SECRET_KEY
            string expectedSignature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(voteId));
                expectedSignature = Convert.ToHexString(hashBytes).ToLower();
            }

            return TimingSafeEqual(providedSignature, expectedSignature);
        }

        /// <summary>
        /// Verifiser ALLE stemmer i databasen (etter valget)
        /// </summary>
        public async Task<VerificationResult> VerifyAllVotes()
        {
            var allVotes = await _context.Vertifikasjons.ToListAsync();
            
            int totalVotes = allVotes.Count;
            int validVotes = 0;
            int invalidVotes = 0;
            var invalidTokens = new List<string>();

            foreach (var vote in allVotes)
            {
                bool isValid = VerifySingleVote(vote.StemmeToken);
                
                if (isValid)
                {
                    validVotes++;
                }
                else
                {
                    invalidVotes++;
                    invalidTokens.Add(vote.StemmeToken);
                }
            }

            return new VerificationResult
            {
                TotalVotes = totalVotes,
                ValidVotes = validVotes,
                InvalidVotes = invalidVotes,
                InvalidTokens = invalidTokens
            };
        }

        private bool TimingSafeEqual(string a, string b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }

    // Resultat-klasse for verifisering
    public class VerificationResult
    {
        public int TotalVotes { get; set; }
        public int ValidVotes { get; set; }
        public int InvalidVotes { get; set; }
        public List<string> InvalidTokens { get; set; }
    }
}