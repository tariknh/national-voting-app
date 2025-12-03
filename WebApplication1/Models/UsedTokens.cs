// Models/UsedToken.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("used_tokens")]
    public class UsedToken
    {
        [Key]
        [Column("vote_id")]
        [MaxLength(64)]
        public string VoteId { get; set; }

        [Column("used_at")]
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}