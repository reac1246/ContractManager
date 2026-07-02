using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using ContractManager.Models;
using ContractManager.Services;

namespace ContractManager.Views
{
    public partial class VerificationDetailsDialog : Window
    {
        private string? _tempAudioPath;
        private SoundPlayer? _player;

        public VerificationDetailsDialog(ContractAssignment assignment)
        {
            InitializeComponent();

            bool hasData = false;

            if (!string.IsNullOrEmpty(assignment.AudioRecordingBase64))
            {
                AudioSection.Visibility = Visibility.Visible;
                AudioHashText.Text = $"SHA-256: {assignment.AudioHash}";
                
                try
                {
                    byte[] audioBytes = Convert.FromBase64String(assignment.AudioRecordingBase64);
                    _tempAudioPath = Path.Combine(Path.GetTempPath(), $"playback_{Guid.NewGuid()}.wav");
                    File.WriteAllBytes(_tempAudioPath, audioBytes);
                    _player = new SoundPlayer(_tempAudioPath);
                }
                catch { /* handle error */ }

                hasData = true;
            }

            if (!string.IsNullOrEmpty(assignment.IdDocumentEncryptedBase64) && !string.IsNullOrEmpty(assignment.IdDocumentEncryptedAesKey))
            {
                IdDocSection.Visibility = Visibility.Visible;
                try
                {
                    byte[] decryptedData = CryptoService.DecryptIdDocument(assignment.IdDocumentEncryptedBase64, assignment.IdDocumentEncryptedAesKey);
                    
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(decryptedData))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.UriSource = null;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    image.Freeze();
                    IdImage.Source = image;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"本人確認書類の復号に失敗しました: {ex.Message}");
                }

                hasData = true;
            }

            if (!hasData)
            {
                NoDataText.Visibility = Visibility.Visible;
            }
        }

        private void PlayAudio_Click(object sender, RoutedEventArgs e)
        {
            _player?.Play();
        }

        private void StopAudio_Click(object sender, RoutedEventArgs e)
        {
            _player?.Stop();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _player?.Stop();
            _player?.Dispose();
            if (_tempAudioPath != null && File.Exists(_tempAudioPath))
            {
                try { File.Delete(_tempAudioPath); } catch { }
            }
            base.OnClosed(e);
        }
    }
}
