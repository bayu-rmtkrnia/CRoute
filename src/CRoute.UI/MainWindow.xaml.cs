using System.Windows;

namespace CRoute.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            tbUserName.Text = Session.FullName;
            tbInitial.Text = Session.Initial;
            NavigateTo("dashboard");   
        }
        public void NavigateTo(string page)
        {
            switch (page)
            {
                case "route":
                    navRoute.IsChecked = true;
                    contentHost.Content = new RouteSearchView();
                    break;
                case "history":
                    navHistory.IsChecked = true;
                    contentHost.Content = new HistoryView();
                    break;
                case "stats":
                    navStats.IsChecked = true;
                    contentHost.Content = new StatsView();
                    break;
                default:
                    navDashboard.IsChecked = true;
                    contentHost.Content = new DashboardView();
                    break;
            }
        }

        private void navDashboard_Click(object sender, RoutedEventArgs e) { NavigateTo("dashboard"); }
        private void navRoute_Click(object sender, RoutedEventArgs e) { NavigateTo("route"); }
        private void navHistory_Click(object sender, RoutedEventArgs e) { NavigateTo("history"); }
        private void navStats_Click(object sender, RoutedEventArgs e) { NavigateTo("stats"); }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.FullName = "Tamu";
            new LoginWindow().Show();
            this.Close();
        }
    }
}
