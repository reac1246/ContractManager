using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ContractManager.Services
{
    public static class AudioRecordService
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int mciSendString(string lpstrCommand, StringBuilder? lpstrReturnString, int uReturnLength, IntPtr hwndCallback);

        private static string _tempWavPath = string.Empty;
        private static bool _isRecording = false;

        public static void StartRecording()
        {
            if (_isRecording) return;

            _tempWavPath = Path.Combine(Path.GetTempPath(), $"contract_audio_{Guid.NewGuid()}.wav");
            
            mciSendString("open new Type waveaudio Alias recsound", null, 0, IntPtr.Zero);
            mciSendString("record recsound", null, 0, IntPtr.Zero);
            
            _isRecording = true;
        }

        public static byte[] StopRecording()
        {
            if (!_isRecording) return Array.Empty<byte>();

            mciSendString($"save recsound \"{_tempWavPath}\"", null, 0, IntPtr.Zero);
            mciSendString("close recsound", null, 0, IntPtr.Zero);

            _isRecording = false;

            if (File.Exists(_tempWavPath))
            {
                byte[] audioData = File.ReadAllBytes(_tempWavPath);
                try { File.Delete(_tempWavPath); } catch { /* ignore */ }
                return audioData;
            }

            return Array.Empty<byte>();
        }
    }
}
