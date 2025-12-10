using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VotingVerifier
{
    public class VotingDbContext : DbContext
    {
        public VotingDbContext(DbContextOptions<VotingDbContext> options) : base(options)
        {
        }

        public DbSet<Vertifikasjon> Vertifikasjons { get; set; }
        public DbSet<Stemmer> Stemmers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vertifikasjon>().ToTable("verifikasjon");
            modelBuilder.Entity<Stemmer>().ToTable("stemmer");
        }
    }

    public class Vertifikasjon
    {
        [Column("id")]
        public int Id { get; set; }
        
        [Column("parti")]
        public int Parti { get; set; }
        
        [Column("stemmetoken")]
        public string StemmeToken { get; set; } = string.Empty;
        
        [Column("kommune")]
        public string Kommune { get; set; } = string.Empty;
    }

    public class Stemmer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("kommune")]
        public string Kommune { get; set; } = string.Empty;
        
        [Column("ap")]
        public int Ap { get; set; }
        
        [Column("hoyre")]
        public int Hoyre { get; set; }
        
        [Column("sp")]
        public int Sp { get; set; }
        
        [Column("frp")]
        public int Frp { get; set; }
        
        [Column("sv")]
        public int Sv { get; set; }
        
        [Column("rodt")]
        public int Rodt { get; set; }
        
        [Column("venstre")]
        public int Venstre { get; set; }
        
        [Column("krf")]
        public int Krf { get; set; }
        
        [Column("mdg")]
        public int Mdg { get; set; }
        
        [Column("inp")]
        public int Inp { get; set; }
        
        [Column("pdk")]
        public int Pdk { get; set; }
        
        [Column("demokratene")]
        public int Demokratene { get; set; }
        
        [Column("liberalistene")]
        public int Liberalistene { get; set; }
        
        [Column("pensjonistpartiet")]
        public int Pensjonistpartiet { get; set; }
        
        [Column("kystpartiet")]
        public int Kystpartiet { get; set; }
        
        [Column("alliansen")]
        public int Alliansen { get; set; }
        
        [Column("nkp")]
        public int Nkp { get; set; }
        
        [Column("piratpartiet")]
        public int Piratpartiet { get; set; }
        
        [Column("helsepartiet")]
        public int Helsepartiet { get; set; }
        
        [Column("folkestyret")]
        public int Folkestyret { get; set; }
        
        [Column("norsk_republikansk_allianse")]  
        public int NorskRepublikanskAllianse { get; set; }
        
        [Column("verdipartiet")]
        public int Verdipartiet { get; set; }
        
        [Column("partiet_sentrum")]  // ← UNDERSCORE
        public int PartietSentrum { get; set; }
    }
}