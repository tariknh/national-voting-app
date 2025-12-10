// Models/Vertifikasjon.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("verifikasjon")]
    public class Vertifikasjon
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("parti")]
        public int Parti { get; set; }

        [Column("stemmetoken")]
        [MaxLength(255)]
        public string StemmeToken { get; set; }

        [Column("kommune")]
        [MaxLength(255)]
        public string? Kommune { get; set; }
    }
}