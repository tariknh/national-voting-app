using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace VotingVerifier
{
    public class VotingVerifierService
    {
        private readonly VotingDbContext _context;
        private readonly string _secretKey;
        private readonly HashSet<string> _generatedTokensInSession;

        public VotingVerifierService(VotingDbContext context)
        {
            _context = context;
            _generatedTokensInSession = new HashSet<string>();
            
            _secretKey = Environment.GetEnvironmentVariable("VOTING_SECRET_KEY");
            
            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException(
                    "VOTING_SECRET_KEY ikke funnet i environment variables!"
                );
            }
        }

        /// <summary>
        /// Generer en gyldig token MED duplikatsjekk
        /// </summary>
        public string GenerateToken()
        {
            const int maxAttempts = 100;
            int attempt = 0;
            
            while (attempt < maxAttempts)
            {
                byte[] randomBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                string voteId = Convert.ToHexString(randomBytes).ToLower();

                string fullToken;
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
                {
                    byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(voteId));
                    string signature = Convert.ToHexString(hashBytes).ToLower();
                    fullToken = $"{voteId}.{signature}";
                }

                // Sjekk om token allerede er generert i denne sesjonen
                if (!_generatedTokensInSession.Contains(fullToken))
                {
                    _generatedTokensInSession.Add(fullToken);
                    return fullToken;
                }
                
                attempt++;
                Console.WriteLine($" Duplikat token generert (forsøk {attempt}), genererer ny...");
            }

            throw new InvalidOperationException(
                $"Kunne ikke generere unik token etter {maxAttempts} forsøk. Dette burde aldri skje!"
            );
        }

        /// <summary>
        /// Generer en gyldig token MED database-sjekk (tregere men 100% sikker)
        /// </summary>
        public async Task<string> GenerateUniqueTokenAsync()
        {
            const int maxAttempts = 100;
            int attempt = 0;
            
            while (attempt < maxAttempts)
            {
                byte[] randomBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                string voteId = Convert.ToHexString(randomBytes).ToLower();

                string fullToken;
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
                {
                    byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(voteId));
                    string signature = Convert.ToHexString(hashBytes).ToLower();
                    fullToken = $"{voteId}.{signature}";
                }

                // Sjekk BÅDE session-cache OG database
                if (!_generatedTokensInSession.Contains(fullToken))
                {
                    bool existsInDb = await _context.Vertifikasjons
                        .AnyAsync(v => v.StemmeToken == fullToken);
                    
                    if (!existsInDb)
                    {
                        _generatedTokensInSession.Add(fullToken);
                        return fullToken;
                    }
                }
                
                attempt++;
                Console.WriteLine($" Duplikat token funnet (forsøk {attempt}), genererer ny...");
            }

            throw new InvalidOperationException(
                $"Kunne ikke generere unik token etter {maxAttempts} forsøk. Dette burde aldri skje!"
            );
        }

        /// <summary>
        /// Verifiser EN stemme
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

            string expectedSignature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(voteId));
                expectedSignature = Convert.ToHexString(hashBytes).ToLower();
            }

            return TimingSafeEqual(providedSignature, expectedSignature);
        }

        /// <summary>
        /// Verifiser ALLE stemmer (med duplikatsjekk)
        /// </summary>
        public async Task<VerificationResult> VerifyAllVotes()
        {
            var votes = await _context.Vertifikasjons.ToListAsync();
            var result = new VerificationResult();

            result.TotalVotes = votes.Count;

            // 1. Grupper stemmer etter token for å finne duplikater
            var groupedVotes = votes
                .GroupBy(v => v.StemmeToken)
                .ToList();

            // 2. Identifiser duplikater: Alle forekomster ETTER den første
            var duplicates = groupedVotes
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Skip(1))
                .ToList();
            
            // 3. De unike tokenene som skal HMAC-verifiseres (første forekomst)
            var uniqueVotesToVerify = groupedVotes
                .Select(g => g.First())
                .ToList();

            if (duplicates.Any())
            {
                // Legg til duplikatene i resultatet
                result.InvalidVotes += duplicates.Count;
                result.InvalidTokens.AddRange(duplicates.Select(d => $"{d.StemmeToken} (Duplikat)"));

                Console.WriteLine($"\n FUNNET {duplicates.Count} DUPLISERTE TOKENS! De er registrert som ugyldige.");
                foreach (var dupGroup in groupedVotes.Where(g => g.Count() > 1))
                {
                    Console.WriteLine($"  Token brukt {dupGroup.Count()} ganger: {dupGroup.Key}");
                }
            }

            // 4. Utfør HMAC-verifisering KUN på de unike tokenene
            foreach (var vote in uniqueVotesToVerify)
            {
                // Bruker VerifySingleVote for å sjekke signaturen
                if (VerifySingleVote(vote.StemmeToken))
                {
                    result.ValidVotes++;
                }
                else
                {
                    result.InvalidVotes++;
                    result.InvalidTokens.Add(vote.StemmeToken); // Dette er en ugyldig signatur
                }
            }

            return result;
        }

        /// <summary>
        /// Fjern duplikater fra databasen (beholder bare første forekomst)
        /// </summary>
        public async Task<int> RemoveDuplicateTokensAsync()
        {
            var votes = await _context.Vertifikasjons.ToListAsync();
            
            // Grupper etter token
            var duplicateGroups = votes
                .GroupBy(v => v.StemmeToken)
                .Where(g => g.Count() > 1)
                .ToList();

            int removedCount = 0;

            foreach (var group in duplicateGroups)
            {
                // Behold første, fjern resten
                var toRemove = group.Skip(1).ToList();
                _context.Vertifikasjons.RemoveRange(toRemove);
                removedCount += toRemove.Count;
                
                Console.WriteLine($" Fjerner {toRemove.Count} duplikater av token: {group.Key.Substring(0, 20)}...");
            }

            if (removedCount > 0)
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"\n Fjernet totalt {removedCount} duplikate tokens fra databasen.");
            }
            else
            {
                Console.WriteLine("\n Ingen duplikater funnet!");
            }

            return removedCount;
        }

        /// <summary>
        /// Rydd cache (kall denne hvis du starter en ny batch med stemmer)
        /// </summary>
        public void ClearTokenCache()
        {
            _generatedTokensInSession.Clear();
            Console.WriteLine(" Token-cache tømt.");
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

    public class VerificationResult
    {
        public int TotalVotes { get; set; }
        public int ValidVotes { get; set; }
        public int InvalidVotes { get; set; }
        public List<string> InvalidTokens { get; set; } = new();
    }
}