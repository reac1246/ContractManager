using System;
using System.Windows;
using ContractManager.Services;

namespace ContractManager.Views
{
    public partial class FirstAdminDialog : Window
    {
        public string Username => UsernameBox.Text.Trim();
        public string Password => PasswordBox.Password;

        public FirstAdminDialog()
        {
            InitializeComponent();
            UsernameBox.Focus();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Username))
            {
                MessageBox.Show("管理者IDを入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                MessageBox.Show("パスワードを入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Password != PasswordConfirmBox.Password)
            {
                MessageBox.Show("パスワードが一致しません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (success, message) = AuthService.CreateAdminFromConsole(Username, Password);
            if (success)
            {
                MessageBox.Show("管理者アカウントを作成しました。\nこれ以降はログイン画面からログインできます。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
