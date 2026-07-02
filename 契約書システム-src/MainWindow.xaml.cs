using System.Windows;
using System.Windows.Controls;
using ContractManager.Services;
using ContractManager.Views;

namespace ContractManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowLoginView();
        }

        public void ShowLoginView()
        {
            MainContent.Children.Clear();
            var loginView = new LoginView(this);
            MainContent.Children.Add(loginView);
        }

        public void ShowDashboard()
        {
            MainContent.Children.Clear();
            
            if (AuthService.IsAdmin)
            {
                var adminDashboard = new AdminDashboard(this);
                MainContent.Children.Add(adminDashboard);
            }
            else
            {
                var userDashboard = new UserDashboard(this);
                MainContent.Children.Add(userDashboard);
            }
        }

        public void Logout()
        {
            AuthService.Logout();
            ShowLoginView();
        }
    }
}
