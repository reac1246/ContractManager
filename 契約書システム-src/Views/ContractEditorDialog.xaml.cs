using System.Windows;
using ContractManager.Models;

namespace ContractManager.Views
{
    public partial class ContractEditorDialog : Window
    {
        public string ContractTitle => TitleBox.Text.Trim();
        public string ContractContent => ContentBox.Text.Trim();
        public bool IsImportantContract => IsImportantCheckBox.IsChecked ?? false;
        public string ExplanationHtml => ExplanationHtmlBox.Text.Trim();

        public ContractEditorDialog(Contract? contract = null)
        {
            InitializeComponent();
            
            if (contract != null)
            {
                Title = "契約書編集";
                TitleBox.Text = contract.Title;
                ContentBox.Text = contract.Content;
                IsImportantCheckBox.IsChecked = contract.IsImportantContract;
                ExplanationHtmlBox.Text = contract.ExplanationHtml;
            }
            else
            {
                Title = "新規契約書作成";
            }
            
            TitleBox.Focus();
        }

        private void IsImportantCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (ExplanationPanel != null)
            {
                ExplanationPanel.Visibility = (IsImportantCheckBox.IsChecked == true) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ContractTitle))
            {
                MessageBox.Show("タイトルを入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(ContractContent))
            {
                MessageBox.Show("契約内容を入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
