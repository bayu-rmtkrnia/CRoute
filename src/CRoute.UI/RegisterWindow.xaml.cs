using System.Windows;

namespace CRoute.UI
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            tbError.Visibility = Visibility.Collapsed;

            string fullName = tbFullName.Text.Trim();
            string username = tbUsername.Text.Trim();
            string password = pbPassword.Password;
            string confirm = pbConfirm.Password;

            // Validasi berurutan, berhenti di kesalahan pertama
            if (fullName.Length == 0 || username.Length == 0 || password.Length == 0)
            {
                ShowError("Semua kolom wajib diisi.");
                return;
            }
            if (password.Length < 6)
            {
                ShowError("Password minimal 6 karakter.");
                return;
            }
            if (password != confirm)
            {
                ShowError("Konfirmasi password tidak sama dengan password.");
                return;
            }
            if (!AccountStore.Register(fullName, username, password))
            {
                ShowError("Username sudah dipakai, coba yang lain.");
                return;
            }

            MessageBox.Show("Akun berhasil dibuat. Silakan login.", "CRoute",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            BackToLogin();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            BackToLogin();
        }

        private void BackToLogin()
        {
            new LoginWindow().Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            tbError.Text = message;
            tbError.Visibility = Visibility.Visible;
        }
    }
}
