using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;
using System.IO;
using System.Linq;

class PlayerStats
{
    public string Player { get; set; }
    public string Team { get; set; }
    public string Pos { get; set; }
    public int GP { get; set; } // Games Played
    public int G { get; set; }  // Goals
    public int A { get; set; }  // Assists
    public int P { get; set; }  // Points

    [Name("+/-")]
    public int PlusMinus { get; set; }//+/- column
    public int PIM { get; set; }
    [Name("P/GP")]
    public double PenaltiesPerGame { get; set; }
    public double PPG { get; set; }
    public double PPP { get; set; }
    public int SHG { get; set; }
    public int SHP { get; set; }
    public int GWG { get; set; }
    public int OTG { get; set; }
    public int S { get; set; }
    [Name("S%")]
    public double Spercentage { get; set; }
    [Name("FOW%")]
    public double FOW { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        string filePath = "players.csv"; // Ensure this file is in the same directory

        // Load data from the csv file
        var players = LoadPlayersFromCsv(filePath);

        if (players.Count == 0)
        {
            Console.WriteLine("No data found in the file");
            return;
        }

        int option;
        do
        {
            // Main menu
            Console.WriteLine("\n--- NHL Player Stats ---");
            Console.WriteLine("1. Show All Data");
            Console.WriteLine("2. Filter Data");
            Console.WriteLine("3. Sort Data");
            Console.WriteLine("4. Exit");
            Console.Write("Choose an option: ");
            option = int.Parse(Console.ReadLine() ?? "4");

            switch (option)
            {
                case 1:
                    ShowData(players); // Show all data
                    break;
                case 2:
                    FilterData(players); // Filter data
                    break;
                case 3:
                    SortData(players); // Sort data
                    break;
                case 4:
                    Console.WriteLine("Goodbye!");
                    break;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }
        } while (option != 4);
    }

    // Load player data from the file
    static List<PlayerStats> LoadPlayersFromCsv(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            });

            var players = csv.GetRecords<PlayerStats>()
                             .Where(p => !string.IsNullOrWhiteSpace(p.Player) && p.GP > 0)
                             .ToList();

            return players;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading the CSV file: {ex.Message}");
            return new List<PlayerStats>();
        }
    }

    // Display player data in a table format
    static void ShowData(List<PlayerStats> players)
    {
        Console.WriteLine("\n--- Player Stats ---");
        Console.WriteLine($"{"Name",-25} {"Team",-10} {"Pos",-3} {"GP",-3} {"G",-3} {"A",-3} {"P",-3} {"+/-",-3} {"PIM",-3} {"P/PG",-5} {"PPG",-5} {"PPP",-4} {"SHG",-5} {"SHP",-4} {"GWG",-5} {"OTG",-5}{"S",-5} {"S%",-5} {"FOW%",-5}");
        foreach (var player in players)
        {
            Console.WriteLine($"{player.Player,-25} {player.Team,-10} {player.Pos,-3} {player.GP,-3} {player.G,-3} {player.A,-3} {player.P,-3} {player.PlusMinus,-3} {player.PIM,-3} {player.PenaltiesPerGame,-5} {player.PPG,-5} {player.PPP,-5}{player.SHG,-5} {player.SHP,-5}{player.GWG,-5} {player.OTG,-5}{player.S,-5} {player.Spercentage,-5} {player.FOW,-5}");
        }
    }

    // Filter player data based on multiple fields and conditions
    static void FilterData(List<PlayerStats> players)
    {
        Console.WriteLine("Enter the conditions to filter (e.g., G > 50 , GP > 10): ");
        string input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("No conditions entered");
            return;
        }

        var conditions = input.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(c => c.Trim())
                              .ToList();

        var filteredPlayers = players;

        foreach (var condition in conditions)
        {
            var parts = System.Text.RegularExpressions.Regex.Match(condition, @"([\w\+\-/]+)\s*(>=|<=|>|<|==|!=)\s*(\d+)");
            if (!parts.Success)
            {
                Console.WriteLine($"Invalid condition: {condition}");
                return;
            }

            string field = parts.Groups[1].Value;
            string operation = parts.Groups[2].Value;
            if (!double.TryParse(parts.Groups[3].Value, out double filterValue))
            {
                Console.WriteLine($"Invalid value in condition: {condition}");
                return;
            }

            if (field == "+/-") field = "PlusMinus";
            if (field == "P/GP") field = "PenaltiesPerGame";
            if (field == "S%") field = "Spercentage";
            if (field == "FOW%") field = "FOW";

            var prop = typeof(PlayerStats).GetProperties()
                .FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));

            if (prop == null)
            {
                Console.WriteLine($"Invalid field: {field}");
                return;
            }

            filteredPlayers = filteredPlayers.Where(player =>
            {
                var value = Convert.ToDouble(prop.GetValue(player));
                return operation switch
                {
                    ">" => value > filterValue,
                    ">=" => value >= filterValue,
                    "<" => value < filterValue,
                    "<=" => value <= filterValue,
                    "==" => value == filterValue,
                    "!=" => value != filterValue,
                    _ => false
                };
            }).ToList();
        }

        if (!filteredPlayers.Any())
        {
            Console.WriteLine("No players found matching the filters.");
        }
        else
        {
            ShowData(filteredPlayers);
        }
    }

    // Sort player data based on a selected field and order
    static void SortData(List<PlayerStats> players)
    {
        Console.WriteLine("Enter the field to sort by (e.g., G, A, P, GP, etc.): ");
        string field = Console.ReadLine()?.Trim();

        Console.WriteLine("Enter sort order (asc for ascending, desc for descending): ");
        string sortOrder = Console.ReadLine()?.ToLower().Trim();

        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(sortOrder))
        {
            Console.WriteLine("Invalid input. Sort canceled.");
            return;
        }

        if (field == "+/-") field = "PlusMinus";
        if (field == "P/GP") field = "PenaltiesPerGame";
        if (field == "S%") field = "Spercentage";
        if (field == "FOW%") field = "FOW";

        var prop = typeof(PlayerStats).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));

        if (prop == null)
        {
            Console.WriteLine($"Invalid field: {field}");
            return;
        }

        var sortedPlayers = sortOrder == "asc"
            ? players.OrderBy(player => prop.GetValue(player)).ToList()
            : players.OrderByDescending(player => prop.GetValue(player)).ToList();

        Console.WriteLine($"Players sorted by {field} in {sortOrder} order:");
        ShowData(sortedPlayers);
    }
}
