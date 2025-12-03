using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class VoteViewModel
    {
        public string? FullName { get; set; }
        public string? Kommune { get; set; }
        public bool HasVoted { get; set; }
        public IReadOnlyList<string> Parties { get; set; } = new List<string>();
        public string? StatusMessage { get; set; }
    }
}
