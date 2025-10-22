using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models


{
    [Table("stemmer")]
    public class Stemmer
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("kommune")]
        public string? Kommune { get; set; }

        [Column("ap")]
        public int Ap { get; set; } = 0;

        [Column("hoyre")]
        public int Hoyre { get; set; } = 0;

        [Column("sp")]
        public int Sp { get; set; } = 0;

        [Column("frp")]
        public int Frp { get; set; } = 0;

        [Column("sv")]
        public int Sv { get; set; } = 0;

        [Column("rodt")]
        public int Rodt { get; set; } = 0;

        [Column("venstre")]
        public int Venstre { get; set; } = 0;

        [Column("krf")]
        public int Krf { get; set; } = 0;

        [Column("mdg")]
        public int Mdg { get; set; } = 0;

        [Column("inp")]
        public int Inp { get; set; } = 0;

        [Column("pdk")]
        public int Pdk { get; set; } = 0;

        [Column("demokratene")]
        public int Demokratene { get; set; } = 0;

        [Column("liberalistene")]
        public int Liberalistene { get; set; } = 0;

        [Column("pensjonistpartiet")]
        public int Pensjonistpartiet { get; set; } = 0;

        [Column("kystpartiet")]
        public int Kystpartiet { get; set; } = 0;

        [Column("alliansen")]
        public int Alliansen { get; set; } = 0;

        [Column("nkp")]
        public int Nkp { get; set; } = 0;

        [Column("piratpartiet")]
        public int Piratpartiet { get; set; } = 0;

        [Column("helsepartiet")]
        public int Helsepartiet { get; set; } = 0;

        [Column("folkestyret")]
        public int Folkestyret { get; set; } = 0;

        [Column("norsk_republikansk_allianse")]
        public int NorskRepublikanskAllianse { get; set; } = 0;

        [Column("verdipartiet")]
        public int Verdipartiet { get; set; } = 0;

        [Column("partiet_sentrum")]
        public int PartietSentrum { get; set; } = 0;

        [Column("frpu")]
        public int Frpu { get; set; } = 0;
    }
}