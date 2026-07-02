using System.Windows;

namespace ContractManager.Views
{
    public partial class DisputeDialog : Window
    {
        public string DisputeReason => ReasonBox.Text.Trim();

        public DisputeDialog(string title = "異議申し立て", string label = "異議申し立て内容を入力してください：")
        {
            InitializeComponent();
            Title = title;
            ReasonLabel.Text = label;
            ReasonBox.Focus();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DisputeReason))
            {
                MessageBox.Show("理由を入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
