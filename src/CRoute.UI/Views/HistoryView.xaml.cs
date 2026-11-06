using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CRoute.UI
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
            LoadTrips();
        }

        private MainWindow? Shell
        {
            get { return Window.GetWindow(this) as MainWindow; }
        }

        private void LoadTrips()
        {
            int count = TripStore.Trips.Count;
            tbCount.Text = count + " perjalanan tersimpan";
            icHistory.ItemsSource = TripStore.Trips.Select(TripRow.From).ToList();

            bool empty = count == 0;
            svList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            pnlEmpty.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            btnClear.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show(
                "Hapus semua riwayat perjalanan? Tindakan ini tidak bisa dibatalkan.",
                "CRoute", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Yes)
            {
                TripStore.Trips.Clear();
                LoadTrips();
            }
        }

        private void btnGoSearch_Click(object sender, RoutedEventArgs e)
        {
            Shell?.NavigateTo("route");
        }
    }
}
