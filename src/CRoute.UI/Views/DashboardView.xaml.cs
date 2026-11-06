using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CRoute.UI
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            tbGreeting.Text = "Halo, " + Session.FirstName + "!";
            tbDate.Text = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("id-ID"));

            double total = TripStore.MonthTotal();
            double target = TripStore.MonthlyTargetKg;
            double percent = target > 0 ? total / target * 100 : 0;

            tbMonthTotal.Text = total.ToString("0.0");
            tbSaved.Text = TripStore.Trips.Sum(t => t.SavedKg).ToString("0.0");
            tbTripCount.Text = TripStore.Trips.Count.ToString();

            pbTarget.Value = Math.Min(percent, 100);
            tbTargetDetail.Text = total.ToString("0.0") + " dari " + target.ToString("0.0") +
                                  " kg terpakai (" + percent.ToString("0") + "%)";
            tbTargetStatus.Text = percent < 70 ? "Masih aman, pertahankan!"
                                : percent < 100 ? "Hampir mencapai target bulan ini"
                                : "Target bulan ini sudah terlampaui";

            icRecent.ItemsSource = TripStore.Trips.Take(3).Select(TripRow.From).ToList();
            tbEmptyRecent.Visibility = TripStore.Trips.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private MainWindow? Shell
        {
            get { return Window.GetWindow(this) as MainWindow; }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Shell?.NavigateTo("route");
        }

        private void btnTarget_Click(object sender, RoutedEventArgs e)
        {
            Shell?.NavigateTo("stats");
        }

        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            Shell?.NavigateTo("history");
        }
    }
}
