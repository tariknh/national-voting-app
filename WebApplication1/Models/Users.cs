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

        [Column("kommune")]
        public string? Kommune { get; set; }
        
        [Column("phonenr")]
        public string? Phonenr { get; set; }
        
        [Column("fodselsnr", TypeName = "varchar(255)")]
        
        public string? Fodselsnr { get; set; }
        
      
    
        [Column("hasvoted")]
        public bool? HasVoted { get; set; }
        [Column("bankiduuid")]
        public string? BankIdUuid { get; set; }
    }
}