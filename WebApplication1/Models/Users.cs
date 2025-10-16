using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("users")]
    public class User
    {
        [Column("id")]
        public int Id { get; set; }
        
        [Column("firstname")]
        public string? Firstname { get; set; }
        
        [Column("lastname")]
        public string? Lastname { get; set; }
        
        [Column("sex")]
        public string? Sex { get; set; }
        
        [Column("adresse")]
        public string? Adresse { get; set; }
        
        [Column("postnr")]
        public string? Postnr { get; set; }
        
        [Column("city")]
        public string? City { get; set; }
        
        [Column("phonenr")]
        public string? Phonenr { get; set; }
        
        [Column("fodselsnr")]
        public string? Fodselsnr { get; set; }
        
        [Column("passord")]
        public string? Passord { get; set; }
        
        [Column("email")]
        public string? Email { get; set; }
    }
}