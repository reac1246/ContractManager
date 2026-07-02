using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ContractManager.Views
{
    public partial class SignatureDialog : Window
    {
        public string SignatureName => NameBox.Text.Trim();
        public DateTime SignatureBirthDate => BirthDatePicker.SelectedDate ?? DateTime.MinValue;
        public string SignatureImage { get; private set; } = "";

        public SignatureDialog()
        {
            InitializeComponent();
            NameBox.Focus();
        }

        private void ClearSignature_Click(object sender, RoutedEventArgs e)
        {
            SignatureCanvas.Strokes.Clear();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SignatureName))
            {
                MessageBox.Show("氏名を入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (BirthDatePicker.SelectedDate == null)
            {
                MessageBox.Show("生年月日を選択してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SignatureCanvas.Strokes.Count == 0)
            {
                MessageBox.Show("署名を入力してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Convert signature to base64 PNG
            SignatureImage = ConvertSignatureToBase64();

            DialogResult = true;
            Close();
        }

        private string ConvertSignatureToBase64()
        {
            // Get the bounds of the strokes
            var bounds = SignatureCanvas.Strokes.GetBounds();
            
            // Create a RenderTargetBitmap
            var width = (int)SignatureCanvas.ActualWidth;
            var height = (int)SignatureCanvas.ActualHeight;
            
            if (width == 0 || height == 0) return "";

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            
            // Create a visual to render
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // White background
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
                
                // Draw the strokes
                SignatureCanvas.Strokes.Draw(context);
            }
            
            rtb.Render(visual);

            // Encode as PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
