using System;
using System.Windows;
using ContractManager.Services;

namespace ContractManager.Views
{
    public partial class ExplanationViewerDialog : Window
    {
        public byte[] AudioData { get; private set; } = Array.Empty<byte>();

        public ExplanationViewerDialog(string htmlContent)
        {
            InitializeComponent();
            
            // WebBrowserにHTMLを表示
            string fullHtml = $@"
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; padding: 20px; }}
                </style>
            </head>
            <body>
                {htmlContent}
            </body>
            </html>";
            
            HtmlBrowser.NavigateToString(fullHtml);
            
            // 録音開始
            AudioRecordService.StartRecording();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 画面を閉じるときに録音停止
            AudioData = AudioRecordService.StopRecording();
        }
    }
}
