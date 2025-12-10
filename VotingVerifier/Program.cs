using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

namespace VotingVerifier
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Last .env fil
            var currentDir = Directory.GetCurrentDirectory();
            var envPath = Path.Combine(currentDir, ".env");

            Console.WriteLine($"Current directory: {currentDir}");
            Console.WriteLine($"Looking for .env at: {envPath}");
            Console.WriteLine($"File exists: {File.Exists(envPath)}");

            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                Console.WriteLine(".env loaded");
            }
            else
            {
                Console.WriteLine(".env not found");
                return;
            }

            var secretKey = Environment.GetEnvironmentVariable("VOTING_SECRET_KEY");
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            Console.WriteLine($"Secret key loaded: {!string.IsNullOrEmpty(secretKey)}");
            Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}\n");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("CONNECTION_STRING ikke funnet i .env");
                return;
            }

            Console.WriteLine("=== Voting Verifier ===");
            Console.WriteLine("1. Fyll inn test-stemmer");
            Console.WriteLine("2. Verifiser alle stemmer");
            Console.WriteLine("3. Vis statistikk");
            Console.WriteLine("4. Avslutt");
            Console.WriteLine("5. Vis tabeller i database");
            Console.WriteLine("6. Vis kolonner i verifikasjon-tabell");
            Console.WriteLine("7. Fjern duplikate tokens fra database");
            Console.WriteLine("9. Vis kolonner i stemmer-tabell");
            Console.WriteLine("10. Vis stemmetall i stemmer-tabell");
            Console.Write("\nVelg alternativ: ");

            var choice = Console.ReadLine();

            var optionsBuilder = new DbContextOptionsBuilder<VotingDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            using var context = new VotingDbContext(optionsBuilder.Options);
            var verifier = new VotingVerifierService(context);

            switch (choice)
            {
                case "1":
                    await FillTestVotes(context, verifier);
                    break;
                case "2":
                    await VerifyAllVotes(verifier);
                    break;
                case "3":
                    await ShowStatistics(context);
                    break;
                case "4":
                    Console.WriteLine("Avslutter...");
                    break;
                case "5":
                    await ListTables(context);
                    break;
                case "6":
                    await ShowTableSchema(context);
                    break;
                case "7":
                    await RemoveDuplicates(verifier);
                    break;
                case "9":
                    await ShowStemmerColumns(context);
                    break;
                case "10":
                    await ShowStemmerData(context);
                    break;
                default:
                    Console.WriteLine("Ugyldig valg!");
                    break;
            }
        }

        // Vektet tilfeldig partivelger basert på realistiske størrelser
        static int GetWeightedRandomParty(Random random)
        {
            
            
            double roll = random.NextDouble();
            
            if (roll < 0.90) // 90% store partier
            {
                return random.Next(1, 10); // Parti 1-9
            }
            else // 10% små partier
            {
                return random.Next(10, 21); // Parti 10-20
            }
        }

        static async Task FillTestVotes(VotingDbContext context, VotingVerifierService verifier)
        {
            Console.Write("Hvor mange test-stemmer vil du legge til? ");
            if (!int.TryParse(Console.ReadLine(), out int count))
            {
                Console.WriteLine("Ugyldig tall!");
                return;
            }

            Console.WriteLine($"\nGenererer {count} test-stemmer...");

            var random = new Random();
            
            // ALLE kommuner fra databasen (et utvalg)
            var kommuner = new[] { 
                "Oslo", "Halden", "Moss", "Sarpsborg", "Fredrikstad", "Hvaler", "Råde", 
                "Våler Innlandet", "Skiptvet", "Indre Østfold", "Rakkestad", "Marker", 
                "Aremark", "Bærum", "Asker", "Lillestrøm", "Nordre Follo", "Ullensaker", 
                "Nesodden", "Frogn", "Vestby", "Ås", "Enebakk", "Lørenskog", "Rælingen", 
                "Aurskog-Høland", "Nes", "Gjerdrum", "Nittedal", "Lunner", "Jevnaker", 
                "Nannestad", "Eidsvoll", "Hurdal", "Drammen", "Kongsberg", "Ringerike", 
                "Hole", "Lier", "Øvre Eiker", "Modum", "Krødsherad", "Flå", "Nesbyen", 
                "Gol", "Hemsedal", "Ål", "Hol", "Sigdal", "Flesberg", "Rollag", 
                "Nore og Uvdal", "Kongsvinger", "Hamar", "Lillehammer", "Gjøvik", 
                "Ringsaker", "Løten", "Stange", "Nord-Odal", "Sør-Odal", "Eidskog", 
                "Grue", "Åsnes", "Våler Østfold", "Elverum", "Trysil", "Åmot", 
                "Stor-Elvdal", "Rendalen", "Engerdal", "Tolga", "Tynset", "Alvdal", 
                "Folldal", "Os", "Dovre", "Lesja", "Skjåk", "Lom", "Vågå", "Nord-Fron", 
                "Sel", "Sør-Fron", "Ringebu", "Øyer", "Gausdal", "Østre Toten", 
                "Vestre Toten", "Gran", "Søndre Land", "Nordre Land", "Sør-Aurdal", 
                "Etnedal", "Nord-Aurdal", "Vestre Slidre", "Øystre Slidre", "Vang", 
                "Horten", "Holmestrand", "Tønsberg", "Sandefjord", "Larvik", "Færder", 
                "Porsgrunn", "Skien", "Notodden", "Siljan", "Bamble", "Kragerø", 
                "Drangedal", "Nome", "Midt-Telemark", "Seljord", "Hjartdal", "Tinn", 
                "Kviteseid", "Nissedal", "Fyresdal", "Tokke", "Vinje", "Risør", 
                "Grimstad", "Arendal", "Kristiansand", "Lindesnes", "Farsund", 
                "Flekkefjord", "Gjerstad", "Vegårshei", "Tvedestrand", "Froland", 
                "Lillesand", "Birkenes", "Åmli", "Iveland", "Evje og Hornnes", 
                "Bygland", "Valle", "Bykle", "Vennesla", "Åseral", "Lyngdal", 
                "Hægebostad", "Kvinesdal", "Sirdal", "Eigersund", "Stavanger", 
                "Haugesund", "Sandnes", "Sokndal", "Lund", "Bjerkreim", "Hå", "Klepp", 
                "Time", "Gjesdal", "Sola", "Randaberg", "Strand", "Hjelmeland", 
                "Suldal", "Sauda", "Kvitsøy", "Bokn", "Tysvær", "Karmøy", "Utsira", 
                "Vindafjord", "Bergen", "Kinn", "Etne", "Sveio", "Bømlo", "Stord", 
                "Fitjar", "Tysnes", "Kvinnherad", "Ullensvang", "Eidfjord", "Ulvik", 
                "Voss", "Kvam herad", "Samnanger", "Bjørnafjorden", "Austevoll", 
                "Øygarden", "Askøy", "Vaksdal", "Modalen", "Osterøy", "Alver", 
                "Austrheim", "Fedje", "Masfjorden", "Gulen", "Solund", "Hyllestad", 
                "Høyanger", "Vik", "Sogndal", "Aurland", "Lærdal", "Årdal", "Luster", 
                "Askvoll", "Fjaler", "Sunnfjord", "Bremanger", "Stad", "Gloppen", 
                "Stryn", "Kristiansund", "Molde", "Ålesund", "Vanylven", "Sande", 
                "Ulstein", "Hareid", "Ørsta", "Stranda", "Sykkylven", "Sula", "Giske", 
                "Vestnes", "Rauma", "Aukra", "Averøy", "Gjemnes", "Tingvoll", "Sunndal", 
                "Surnadal", "Smøla", "Aure", "Volda", "Fjord", "Hustadvika", "Haram", 
                "Trondheim", "Steinkjer", "Namsos", "Frøya", "Osen", "Oppdal", 
                "Rennebu", "Røros", "Holtålen", "Midtre Gauldal", "Melhus", "Skaun", 
                "Malvik", "Selbu", "Tydal", "Meråker", "Stjørdal", "Frosta", "Levanger", 
                "Verdal", "Snåsa", "Lierne", "Røyrvik", "Namsskogan", "Grong", 
                "Høylandet", "Overhalla", "Flatanger", "Leka", "Inderøy", "Indre Fosen", 
                "Heim", "Hitra", "Ørland", "Åfjord", "Orkland", "Nærøysund", "Rindal", 
                "Bodø", "Narvik", "Bindal", "Sømna", "Brønnøy", "Vega", "Vevelstad", 
                "Herøy", "Alstahaug", "Leirfjord", "Vefsn", "Grane", "Hattfjelldal", 
                "Dønna", "Nesna", "Hemnes", "Rana", "Lurøy", "Træna", "Rødøy", "Meløy", 
                "Gildeskål", "Beiarn", "Saltdal", "Fauske - Fuossko", "Sørfold", 
                "Steigen", "Lødingen", "Evenes", "Røst", "Værøy", "Flakstad", 
                "Vestvågøy", "Vågan", "Hadsel", "Bø", "Øksnes", "Sortland", "Andøy", 
                "Moskenes", "Hamarøy", "Tromsø", "Harstad", "Kvæfjord", "Tjeldsund", 
                "Ibestad", "Gratangen", "Lavangen", "Bardu", "Salangen", "Målselv", 
                "Sørreisa", "Dyrøy", "Senja", "Balsfjord", "Karlsøy", "Lyngen", 
                "Storfjord", "Kåfjord", "Skjervøy", "Nordreisa", "Kvænangen", "Alta", 
                "Hammerfest", "Sør-Varanger", "Vadsø", "Karasjok", "Kautokeino", "Loppa", 
                "Hasvik", "Måsøy", "Nordkapp", "Porsanger", "Lebesby", "Gamvik", "Tana", 
                "Berlevåg", "Båtsfjord", "Vardø", "Nesseby"
            };
            
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var token = verifier.GenerateToken();
                    
                    // Bruk vektet tilfeldig parti (int mellom 1-20)
                    var partiId = GetWeightedRandomParty(random);
                    
                    var kommune = kommuner[random.Next(kommuner.Length)];

                    // 1. Legg til i Vertifikasjon-tabellen (kryptert) - MED INT
                    var vertifikasjon = new Vertifikasjon
                    {
                        StemmeToken = token,
                        Parti = partiId,  // Alltid int
                        Kommune = kommune
                    };
                    context.Vertifikasjons.Add(vertifikasjon);

                    // 2. Oppdater Stemmers-tabellen (visuell)
                    var stemme = await context.Stemmers.FirstOrDefaultAsync(s => s.Kommune == kommune);
                    if (stemme == null)
                    {
                        Console.WriteLine($" Oppretter ny rad for {kommune}");
                        stemme = new Stemmer { Kommune = kommune };
                        context.Stemmers.Add(stemme);
                    }

                    // Bruk partiId i stedet for partinavn
                    IncrementPartyCounterById(stemme, partiId);
                    
                    // SAVE HVER 10. STEMME
                    if ((i + 1) % 100 == 0)
                    {
                        await context.SaveChangesAsync();
                        Console.WriteLine($"Lagret {i + 1} stemmer...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Feil ved stemme {i + 1}: {ex.Message}");
                }
            }

            // Final save
            await context.SaveChangesAsync();
            Console.WriteLine($"\n Totalt {count} test-stemmer lagt til i BEGGE tabeller!");
        }

        static async Task VerifyAllVotes(VotingVerifierService verifier)
        {
            Console.WriteLine("\n Verifiserer alle stemmer...\n");

            var result = await verifier.VerifyAllVotes();

            Console.WriteLine("=== VERIFIKASJONS-RESULTAT ===");
            Console.WriteLine($"Totale stemmer: {result.TotalVotes}");
            Console.WriteLine($" Gyldige stemmer: {result.ValidVotes}");
            Console.WriteLine($" Ugyldige stemmer: {result.InvalidVotes}");

            if (result.InvalidVotes > 0)
            {
                Console.WriteLine("\n UGYLDIGE TOKENS:");
                foreach (var token in result.InvalidTokens)
                {
                    Console.WriteLine($"  - {token}");
                }
            }
            else
            {
                Console.WriteLine("\n Alle stemmer er gyldige!");
            }
        }

        static async Task ShowStatistics(VotingDbContext context)
        {
            Console.WriteLine("\n STEMME-STATISTIKK\n");

            var totalVotes = await context.Vertifikasjons.CountAsync();
            Console.WriteLine($"Totalt antall stemmer: {totalVotes}");

            var votesByKommune = await context.Vertifikasjons
                .GroupBy(v => v.Kommune)
                .Select(g => new { Kommune = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            Console.WriteLine("\n Topp 10 kommuner:");
            foreach (var item in votesByKommune.Take(10))
            {
                Console.WriteLine($"  {item.Kommune}: {item.Count} stemmer");
            }

            var votesByParty = await context.Vertifikasjons
                .GroupBy(v => v.Parti)
                .Select(g => new { Parti = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            Console.WriteLine("\nStemmer per parti:");
            foreach (var item in votesByParty)
            {
                var partyName = GetPartyName(item.Parti);
                Console.WriteLine($"  {partyName} (ID: {item.Parti}): {item.Count} stemmer");
            }
        }

        static async Task ListTables(VotingDbContext context)
        {
            Console.WriteLine("\n TABELLER I DATABASEN:\n");
            
            try
            {
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public'
                    ORDER BY table_name;
                ";
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  - {reader.GetString(0)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Feil: {ex.Message}");
            }
        }

        static async Task ShowTableSchema(VotingDbContext context)
        {
            Console.WriteLine("\n KOLONNER I verifikasjon-TABELLEN:\n");
    
            try
            {
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
        
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT column_name, data_type 
                    FROM information_schema.columns 
                    WHERE table_name = 'verifikasjon'
                    ORDER BY ordinal_position;
                ";
        
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Feil: {ex.Message}");
            }
        }

        // NY METODE: Bruker parti-ID i stedet for navn
        static void IncrementPartyCounterById(Stemmer stemme, int partiId)
        {
            switch (partiId)
            {
                case 1: stemme.Ap++; break;
                case 2: stemme.Hoyre++; break;
                case 3: stemme.Sp++; break;
                case 4: stemme.Frp++; break;
                case 5: stemme.Sv++; break;
                case 6: stemme.Venstre++; break;
                case 7: stemme.Krf++; break;
                case 8: stemme.Rodt++; break;
                case 9: stemme.Mdg++; break;
                case 10: stemme.Inp++; break;
                case 11: stemme.Pdk++; break;
                case 12: stemme.Demokratene++; break;
                case 13: stemme.Liberalistene++; break;
                case 14: stemme.Pensjonistpartiet++; break;
                case 15: stemme.Kystpartiet++; break;
                case 16: stemme.Alliansen++; break;
                case 17: stemme.Nkp++; break;
                case 18: stemme.Piratpartiet++; break;
                case 19: stemme.Helsepartiet++; break;
                case 20: stemme.Folkestyret++; break;
                default:
                    Console.WriteLine($" Ukjent parti-ID: {partiId}");
                    break;
            }
        }

        static async Task ShowStemmerColumns(VotingDbContext context)
        {
            Console.WriteLine("\n KOLONNER I stemmer-TABELLEN:\n");
    
            try
            {
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
        
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT column_name, data_type 
                    FROM information_schema.columns 
                    WHERE table_name = 'stemmer'
                    ORDER BY ordinal_position;
                ";
        
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Feil: {ex.Message}");
            }
        }

        static async Task ShowStemmerData(VotingDbContext context)
        {
            Console.WriteLine("\n STEMMETALL I STEMMER-TABELLEN:\n");
    
            var stemmers = await context.Stemmers
                .OrderByDescending(s => s.Ap + s.Hoyre + s.Sp + s.Frp + s.Sv)
                .Take(10)
                .ToListAsync();
    
            foreach (var stemme in stemmers)
            {
                var total = stemme.Ap + stemme.Hoyre + stemme.Sp + stemme.Frp + stemme.Sv + 
                            stemme.Rodt + stemme.Venstre + stemme.Krf + stemme.Mdg + stemme.Inp + 
                            stemme.Pdk + stemme.Demokratene + stemme.Liberalistene + 
                            stemme.Pensjonistpartiet + stemme.Kystpartiet + stemme.Alliansen + 
                            stemme.Nkp + stemme.Piratpartiet + stemme.Helsepartiet + 
                            stemme.Folkestyret + stemme.NorskRepublikanskAllianse + 
                            stemme.Verdipartiet + stemme.PartietSentrum;
        
                Console.WriteLine($"{stemme.Kommune}: {total} stemmer (Ap:{stemme.Ap}, H:{stemme.Hoyre}, Sp:{stemme.Sp}, Frp:{stemme.Frp}, Sv:{stemme.Sv})");
            }
    
            var totalVotes = await context.Stemmers.SumAsync(s => 
                s.Ap + s.Hoyre + s.Sp + s.Frp + s.Sv + s.Rodt + s.Venstre + 
                s.Krf + s.Mdg + s.Inp + s.Pdk + s.Demokratene + s.Liberalistene + 
                s.Pensjonistpartiet + s.Kystpartiet + s.Alliansen + s.Nkp + 
                s.Piratpartiet + s.Helsepartiet + s.Folkestyret + 
                s.NorskRepublikanskAllianse + s.Verdipartiet + s.PartietSentrum);
    
            Console.WriteLine($"\n Totalt antall stemmer i Stemmers-tabellen: {totalVotes}");
        }

        static async Task RemoveDuplicates(VotingVerifierService verifier)
        {
            Console.WriteLine("\n FJERNER DUPLIKATE TOKENS...\n");
            
            var removedCount = await verifier.RemoveDuplicateTokensAsync();
            
            if (removedCount > 0)
            {
                Console.WriteLine($"\n Operasjon fullført! {removedCount} duplikater fjernet.");
            }
        }

        static string GetPartyName(int id)
        {
            return id switch
            {
                1 => "Arbeiderpartiet",
                2 => "Høyre",
                3 => "Senterpartiet",
                4 => "Fremskrittspartiet",
                5 => "Sosialistisk Venstreparti",
                6 => "Venstre",
                7 => "Kristelig Folkeparti",
                8 => "Rødt",
                9 => "Miljøpartiet De Grønne",
                10 => "Innsamslingspartiet",
                11 => "Partiet De Kristne",
                12 => "Demokratene",
                13 => "Liberalistene",
                14 => "Pensjonistpartiet",
                15 => "Kystpartiet",
                16 => "Alliansen",
                17 => "Norges Kommunistiske Parti",
                18 => "Piratpartiet",
                19 => "Helsepartiet",
                20 => "Folkestyret",
                _ => $"Parti {id}"
            };
        }
    }
}