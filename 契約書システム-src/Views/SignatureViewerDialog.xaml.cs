using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ContractManager.Views
{
    public partial class SignatureViewerDialog : Window
    {
        public SignatureViewerDialog(string signerName, string base64Image)
        {
            InitializeComponent();
            
            SignerName.Text = $"署名者: {signerName}";
            
            try
            {
                var imageBytes = Convert.FromBase64String(base64Image);
                using var ms = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                SignatureImage.Source = bitmap;
            }
            catch
            {
                MessageBox.Show("署名画像の読み込みに失敗しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
