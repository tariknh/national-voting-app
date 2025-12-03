namespace WebApplication1.Models
{
    public class KommuneStemmeResultat
    {
        public string? KommuneNavn { get; set; } // Dette er det tilpassede navnet (f.eks. hol_buskerud)
        public string? VinnerParti { get; set; }
        public string? VinnerPartiFarge { get; set; }
        public string? VinnerPartiFulltNavn { get; set; }
        public int TotalStemmer { get; set; }
        public Dictionary<string, int>? Detaljer { get; set; }
    }
}