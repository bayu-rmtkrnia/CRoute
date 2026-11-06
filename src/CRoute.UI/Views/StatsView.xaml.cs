using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CRoute.UI
{
    public class DayBar
    {
        public string Label { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
        public double BarHeight { get; set; }
    }

    public partial class StatsView : UserControl
    {
        private static readonly CultureInfo IdCulture = new CultureInfo("id-ID");

        public StatsView()
        {
            InitializeComponent();
            tbTarget.Text = TripStore.MonthlyTargetKg.ToString("0.0");
            Refresh();
        }

        private void Refresh()
        {
            double total = TripStore.MonthTotal();
            double target = TripStore.MonthlyTargetKg;
            double percent = target > 0 ? total / target * 100 : 0;

            tbTotal.Text = total.ToString("0.0") + " kg";
            tbOfTarget.Text = "dari target " + target.ToString("0.0") + " kg (" + percent.ToString("0") + "%)";
            pbTarget.Value = Math.Min(percent, 100);
            tbStatus.Text = percent < 70 ? "Masih aman, pertahankan! 🎉"
                          : percent < 100 ? "Hampir mencapai target, pilih moda yang lebih hemat."
                          : "Target bulan ini sudah terlampaui.";

            var trips = TripStore.Trips;
            tbSavedAll.Text = trips.Sum(t => t.SavedKg).ToString("0.0") + " kg";
            tbAverage.Text = trips.Count > 0
                ? trips.Average(t => t.EmissionKg).ToString("0.00") + " kg"
                : "-";
            tbFavorite.Text = trips.Count > 0
                ? trips.GroupBy(t => t.Mode).OrderByDescending(g => g.Count()).First().Key
                : "-";

            var days = new List<DayBar>();
            var sums = new List<double>();
            for (int i = 6; i >= 0; i--)
            {
                DateTime day = DateTime.Today.AddDays(-i);
                sums.Add(trips.Where(t => t.Date.Date == day).Sum(t => t.EmissionKg));
            }
            double max = sums.Max();
            for (int i = 6; i >= 0; i--)
            {
                DateTime day = DateTime.Today.AddDays(-i);
                double value = sums[6 - i];
                days.Add(new DayBar
                {
                    Label = day.ToString("ddd", IdCulture),
                    ValueText = value > 0 ? value.ToString("0.0") : string.Empty,
                    BarHeight = max > 0 ? Math.Max(3, value / max * 150) : 3
                });
            }
            icBars.ItemsSource = days;
        }

        private void btnSaveTarget_Click(object sender, RoutedEventArgs e)
        {
            string text = tbTarget.Text.Trim().Replace(',', '.');
            bool ok = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value);

            if (!ok || value <= 0)
            {
                ShowMessage("Masukkan angka target yang lebih dari 0.", false);
                return;
            }

            TripStore.MonthlyTargetKg = value;
            Refresh();
            ShowMessage("✓ Target disimpan.", true);
        }

        private void ShowMessage(string message, bool success)
        {
            tbTargetMsg.Text = message;
            tbTargetMsg.Foreground = success
                ? (System.Windows.Media.Brush)FindResource("MidGreenBrush")
                : (System.Windows.Media.Brush)FindResource("DangerBrush");
            tbTargetMsg.Visibility = Visibility.Visible;
        }
    }
}
