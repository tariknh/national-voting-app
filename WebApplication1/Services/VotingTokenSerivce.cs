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
                    "VOTING_SECRET_KEY ikke funnet i .env"
                );
            }
        }
        
        public async Task<string> GenerateVotingTokenByBankId(string bankIdUuid)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.BankIdUuid == bankIdUuid);
            
            if (user == null)
            {
                throw new InvalidOperationException("Bruker ikke funnet");
            }

            if (user.HasVoted == true)
            {
                throw new InvalidOperationException("Already voted");
            }

            // Generer tilfeldig voteId (ikke knyttet til bruker)
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

                // setter sammen til full token
                string fullToken = $"{voteId}.{signature}";

                
                return fullToken;
            }
        }

      
        public async Task StoreVote(string fullToken, int partiInt, string kommune)
        {
            // skjekk for duplikate tokens ved generering
            var tokenExists = await _context.Vertifikasjons
                .AnyAsync(v => v.StemmeToken == fullToken);

            if (tokenExists)
            {
                throw new InvalidOperationException("Token already used");
            }

            // lagre stemme som full token pluss voteid for verifisering
            var vertifikasjon = new Vertifikasjon
            {
                StemmeToken = fullToken,  //token (voteid . full token)
                Parti = partiInt,
                Kommune = kommune
            };

            _context.Vertifikasjons.Add(vertifikasjon);
            await _context.SaveChangesAsync();
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
    
    
}