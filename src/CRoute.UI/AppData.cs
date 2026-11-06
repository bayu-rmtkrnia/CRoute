using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CRoute.UI
{
    public static class Session
    {
        public static string FullName { get; set; } = "Tamu";

        public static string FirstName
        {
            get
            {
                string[] parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? parts[0] : "Tamu";
            }
        }

        public static string Initial
        {
            get { return FullName.Length > 0 ? FullName.Substring(0, 1).ToUpperInvariant() : "T"; }
        }
    }

    public class ModeOption
    {
        public string Name { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;
        public double Factor { get; init; }   
    }

    public class TripRecord
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime Date { get; init; }
        public string Origin { get; init; } = string.Empty;
        public string Destination { get; init; } = string.Empty;
        public double DistanceKm { get; init; }
        public string Mode { get; init; } = string.Empty;
        public double EmissionKg { get; init; }
        public double SavedKg { get; init; }   
    }

    public static class DemoData
    {
        public static readonly ModeOption[] Modes =
        {
            new ModeOption { Name = "Mobil",      Icon = "🚗", Factor = 0.171 },
            new ModeOption { Name = "Motor",      Icon = "🏍", Factor = 0.103 },
            new ModeOption { Name = "Bus",        Icon = "🚌", Factor = 0.089 },
            new ModeOption { Name = "Kereta",     Icon = "🚆", Factor = 0.041 },
            new ModeOption { Name = "Sepeda",     Icon = "🚲", Factor = 0.0 },
            new ModeOption { Name = "Jalan kaki", Icon = "🚶", Factor = 0.0 },
        };

        public static double EstimateDistanceKm(string origin, string destination)
        {
            int sum = 0;
            foreach (char c in (origin.Trim() + "|" + destination.Trim()).ToLowerInvariant())
                sum += c;
            return Math.Round(3 + (sum % 220) / 10.0, 1);
        }
    }

    public static class TripStore
    {
        public static List<TripRecord> Trips { get; } = new List<TripRecord>();
        public static double MonthlyTargetKg { get; set; } = 15.0;

        static TripStore()
        {
            Seed(1, "Kaliurang, Sleman", "UGM, Bulaksumur", 12.4, "Bus");
            Seed(2, "Malioboro", "Stasiun Tugu", 2.1, "Jalan kaki");
            Seed(3, "UGM, Bulaksumur", "Bandara YIA", 42.0, "Kereta");
            Seed(5, "Condongcatur", "UGM, Bulaksumur", 8.3, "Sepeda");
            Seed(6, "Sleman City Hall", "UGM, Bulaksumur", 9.6, "Motor");
        }

        private static void Seed(int daysAgo, string origin, string destination, double km, string modeName)
        {
            ModeOption mode = DemoData.Modes.First(m => m.Name == modeName);
            double car = DemoData.Modes[0].Factor;
            Trips.Add(new TripRecord
            {
                Date = DateTime.Today.AddDays(-daysAgo).AddHours(8),
                Origin = origin,
                Destination = destination,
                DistanceKm = km,
                Mode = modeName,
                EmissionKg = km * mode.Factor,
                SavedKg = Math.Max(0, km * (car - mode.Factor))
            });
        }

        public static void Add(TripRecord trip)
        {
            Trips.Insert(0, trip);   
        }

        public static double MonthTotal()
        {
            DateTime now = DateTime.Now;
            return Trips
                .Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month)
                .Sum(t => t.EmissionKg);
        }
    }

    public class TripRow
    {
        private static readonly CultureInfo IdCulture = new CultureInfo("id-ID");

        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string EmissionText { get; set; } = string.Empty;
        public string SavedText { get; set; } = string.Empty;

        public static TripRow From(TripRecord t)
        {
            ModeOption? mode = DemoData.Modes.FirstOrDefault(m => m.Name == t.Mode);
            return new TripRow
            {
                Icon = mode != null ? mode.Icon : "📍",
                Title = t.Origin + "  →  " + t.Destination,
                Subtitle = t.Date.ToString("d MMM", IdCulture) + "  ·  " +
                           t.DistanceKm.ToString("0.0") + " km  ·  " + t.Mode,
                EmissionText = t.EmissionKg.ToString("0.00") + " kg",
                SavedText = t.SavedKg > 0 ? "hemat " + t.SavedKg.ToString("0.00") + " kg" : string.Empty
            };
        }
    }
}
