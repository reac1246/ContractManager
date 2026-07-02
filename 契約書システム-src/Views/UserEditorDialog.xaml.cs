using System;
using System.Windows;
using ContractManager.Models;

namespace ContractManager.Views
{
    public partial class UserEditorDialog : Window
    {
        private readonly bool _isEditing;
        
        public string Username => UsernameBox.Text.Trim();
        public string Password => PasswordBox.Password;
        public string NameEnglish => NameEnglishBox.Text.Trim();
        public DateTime? BirthDate => BirthDatePicker.SelectedDate;

        public UserEditorDialog(User? user = null)
        {
            InitializeComponent();
            
            if (user != null)
            {
                _isEditing = true;
                Title = "ユーザー編集";
                UsernameBox.Text = user.Username;
                NameEnglishBox.Text = user.NameEnglish;
                BirthDatePicker.SelectedDate = user.BirthDate;
                PasswordLabel.Text = "パスワード（変更する場合のみ）";
            }
            else
            {
                _isEditing = false;
                Title = "新規ユーザー作成";
            }
            
            UsernameBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Username))
            {
                MessageBox.Show("ユーザーIDを入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_isEditing && string.IsNullOrEmpty(Password))
            {
                MessageBox.Show("パスワードを入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
