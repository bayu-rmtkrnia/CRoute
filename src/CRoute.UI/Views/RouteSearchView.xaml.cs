using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CRoute.UI
{
    public class ResultRow
    {
        public string Icon { get; set; } = string.Empty;
        public string ModeName { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public double Percent { get; set; }
        public string EmissionText { get; set; } = string.Empty;
        public Brush BarBrush { get; set; } = Brushes.Green;
    }

    public partial class RouteSearchView : UserControl
    {
        private TripRecord? _pendingTrip;   

        public RouteSearchView()
        {
            InitializeComponent();

            foreach (ModeOption mode in DemoData.Modes)
            {
                wpModes.Children.Add(new ToggleButton
                {
                    Content = mode.Icon + "  " + mode.Name,
                    Tag = mode,
                    IsChecked = true,
                    Style = (Style)FindResource("ChipToggle"),
                    Margin = new Thickness(0, 0, 8, 8)
                });
            }
        }

        private MainWindow? Shell
        {
            get { return Window.GetWindow(this) as MainWindow; }
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            tbError.Visibility = Visibility.Collapsed;

            string origin = tbOrigin.Text.Trim();
            string destination = tbDestination.Text.Trim();

            if (origin.Length == 0 || destination.Length == 0)
            {
                ShowError("Lokasi asal dan tujuan wajib diisi.");
                return;
            }
            if (string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Lokasi asal dan tujuan tidak boleh sama.");
                return;
            }

            var selected = wpModes.Children.OfType<ToggleButton>()
                                  .Where(b => b.IsChecked == true)
                                  .Select(b => (ModeOption)b.Tag)
                                  .ToList();
            if (selected.Count == 0)
            {
                ShowError("Pilih minimal satu moda transportasi.");
                return;
            }

            double km = DemoData.EstimateDistanceKm(origin, destination);
            int minutes = Math.Max(1, (int)Math.Round(km / 28.0 * 60));

            var results = selected.Select(m => new { Mode = m, Kg = km * m.Factor }).ToList();
            var best = results.OrderBy(r => r.Kg).First();
            var worst = results.OrderByDescending(r => r.Kg).First();
            double max = worst.Kg;

            double carKg = km * DemoData.Modes[0].Factor;
            double saved = Math.Max(0, carKg - best.Kg);

            tbRoute.Text = origin + "  →  " + destination;
            tbRouteInfo.Text = km.ToString("0.0") + " km  ·  ± " + minutes + " menit (estimasi)";

            tbRecTitle.Text = "Rekomendasi: " + best.Mode.Icon + " " + best.Mode.Name;
            tbRecDetail.Text = saved > 0
                ? "Emisi " + best.Kg.ToString("0.00") + " kg CO₂, hemat " + saved.ToString("0.00") + " kg dibanding mobil."
                : "Emisi " + best.Kg.ToString("0.00") + " kg CO₂, terendah dari moda yang kamu pilih.";

            icResults.ItemsSource = results
                .OrderByDescending(r => r.Kg)
                .Select(r => new ResultRow
                {
                    Icon = r.Mode.Icon,
                    ModeName = r.Mode.Name,
                    Badge = r.Mode.Name == best.Mode.Name ? "★ terbaik" : string.Empty,
                    Percent = max > 0 ? r.Kg / max * 100 : 0,
                    EmissionText = r.Kg.ToString("0.00") + " kg",
                    BarBrush = BrushFor(max > 0 ? r.Kg / max : 0)
                })
                .ToList();

            _pendingTrip = new TripRecord
            {
                Date = DateTime.Now,
                Origin = origin,
                Destination = destination,
                DistanceKm = km,
                Mode = best.Mode.Name,
                EmissionKg = best.Kg,
                SavedKg = saved
            };

            btnSave.IsEnabled = true;
            btnSave.Content = "Simpan ke riwayat";
            btnHistory.Visibility = Visibility.Collapsed;

            pnlEmpty.Visibility = Visibility.Collapsed;
            pnlResult.Visibility = Visibility.Visible;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingTrip == null)
                return;

            TripStore.Add(_pendingTrip);
            _pendingTrip = null;

            btnSave.IsEnabled = false;
            btnSave.Content = "✓ Tersimpan di riwayat";
            btnHistory.Visibility = Visibility.Visible;
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            Shell?.NavigateTo("history");
        }

        private static Brush BrushFor(double ratio)
        {
            if (ratio > 0.66) return new SolidColorBrush(Color.FromRgb(0xE0, 0x89, 0x4B));
            if (ratio > 0.33) return new SolidColorBrush(Color.FromRgb(0xE8, 0xC5, 0x47));
            return new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x5B));
        }

        private void ShowError(string message)
        {
            tbError.Text = message;
            tbError.Visibility = Visibility.Visible;
        }
    }
}
