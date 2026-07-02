using System.Windows;
using System.Windows.Controls;
using ContractManager.Services;

namespace ContractManager.Views
{
    public partial class LoginView : UserControl
    {
        private readonly MainWindow _mainWindow;

        public LoginView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            UsernameBox.Focus();
            
            this.Loaded += LoginView_Loaded;
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            var users = UserService.GetAllUsers();
            if (users.Count == 0)
            {
                var dialog = new FirstAdminDialog();
                if (dialog.ShowDialog() == true)
                {
                    // Optionally auto-fill the username
                    UsernameBox.Text = dialog.Username;
                    PasswordBox.Focus();
                }
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("ユーザーIDを入力してください");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("パスワードを入力してください");
                return;
            }

            var (success, message) = AuthService.Login(username, password);

            if (success)
            {
                _mainWindow.ShowDashboard();
            }
            else
            {
                ShowError(message);
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
