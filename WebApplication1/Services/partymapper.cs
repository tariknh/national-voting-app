
using System.Collections.Generic;

namespace WebApplication1.Services
{
    public static class PartyMapper
    {
        private static readonly Dictionary<string, int> PartyToInt = new()
        {
            { "arbeiderpartiet", 1 },
            { "høyre", 2 },
            { "senterpartiet", 3 },
            { "fremskrittspartiet", 4 },
            { "sosialistisk venstreparti", 5 },
            { "venstre", 6 },
            { "kristelig folkeparti", 7 },
            { "rødt", 8 },
            { "miljøpartiet de grønne", 9 },
            { "innsamslingspartiet", 10 },
            { "partiet de kristne", 11 },
            { "demokratene", 12 },
            { "liberalistene", 13 },
            { "pensjonistpartiet", 14 },
            { "kystpartiet", 15 },
            { "alliansen", 16 },
            { "norges kommunistiske parti", 17 },
            { "piratpartiet", 18 },
            { "helsepartiet", 19 },
            { "folkestyret", 20 },
            { "norsk republikanse allianse", 21 },
            { "verdipartiet", 22 },
            { "partiet sentrum", 23 },
            { "blank", 24 }
        };

        private static readonly Dictionary<int, string> IntToParty = new()
        {
            { 1, "Arbeiderpartiet" },
            { 2, "Høyre" },
            { 3, "Senterpartiet" },
            { 4, "Fremskrittspartiet" },
            { 5, "Sosialistisk Venstreparti" },
            { 6, "Venstre" },
            { 7, "Kristelig Folkeparti" },
            { 8, "Rødt" },
            { 9, "Miljøpartiet De Grønne" },
            { 10, "Innsamslingspartiet" },
            { 11, "Partiet De Kristne" },
            { 12, "Demokratene" },
            { 13, "Liberalistene" },
            { 14, "Pensjonistpartiet" },
            { 15, "Kystpartiet" },
            { 16, "Alliansen" },
            { 17, "Norges Kommunistiske Parti" },
            { 18, "Piratpartiet" },
            { 19, "Helsepartiet" },
            { 20, "Folkestyret" },
            { 21, "Norsk Republikanse Allianse" },
            { 22, "Verdipartiet" },
            { 23, "Partiet Sentrum" },
            {24, "blank" }
        };

        
        // Konverterer partinavn til int ID
        
        public static int GetPartyId(string partyName)
        {
            var key = partyName.ToLower();
            return PartyToInt.ContainsKey(key) ? PartyToInt[key] : -1;
        }

        
        // Konverterer parti ID til navn
        
        public static string GetPartyName(int partyId)
        {
            return IntToParty.ContainsKey(partyId) ? IntToParty[partyId] : "Ukjent";
        }

      
        // Henter alle partinavn liste
        
        public static List<string> GetAllPartyNames()
        {
            return new List<string>(IntToParty.Values);
        }
        // Sjekk om partinavn er gyldig
        
        public static bool IsValidParty(string partyName)
        {
            return PartyToInt.ContainsKey(partyName.ToLower());
        }
    }
}