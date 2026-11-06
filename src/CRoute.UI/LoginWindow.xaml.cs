using System.Windows;

namespace CRoute.UI
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            tbError.Visibility = Visibility.Collapsed;

            string username = tbUsername.Text.Trim();
            string password = pbPassword.Password;

            if (username.Length == 0 || password.Length == 0)
            {
                ShowError("Username dan password wajib diisi.");
                return;
            }

            string? fullName = AccountStore.Login(username, password);
            if (fullName == null)
            {
                ShowError("Username atau password salah.");
                return;
            }

            Session.FullName = fullName;
            new MainWindow().Show();
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lnkRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            tbError.Text = message;
            tbError.Visibility = Visibility.Visible;
        }
    }
}
