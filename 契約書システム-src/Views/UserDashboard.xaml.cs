using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ContractManager.Models;
using ContractManager.Services;

namespace ContractManager.Views
{
    public partial class UserDashboard : UserControl
    {
        private readonly MainWindow _mainWindow;
        private List<ContractDisplayItem> _contracts = new();
        private ContractDisplayItem? _selectedContract;
        private byte[]? _recordedAudio = null;
        private byte[]? _idDocumentBytes = null;

        public UserDashboard(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            WelcomeText.Text = $"ようこそ、{AuthService.CurrentUser?.Username} さん";
            
            LoadContracts();
        }

        private void LoadContracts()
        {
            var userId = AuthService.CurrentUser?.Id ?? "";
            var assignments = ContractService.GetAssignmentsForUser(userId);
            var allContracts = ContractService.GetAllContracts();

            _contracts = assignments.Select(a =>
            {
                var contract = allContracts.FirstOrDefault(c => c.Id == a.ContractId);
                string statusText = GetStatusText(a.Status);
                // 代理同意の場合はステータス表示に注記を追加
                if (a.Status == ContractStatus.Agreed && a.IsProxyAgreement)
                {
                    statusText = "代理同意済み（署名なし）";
                }
                return new ContractDisplayItem
                {
                    Assignment = a,
                    ContractId = a.ContractId,
                    ContractTitle = contract?.Title ?? "不明",
                    ContractContent = contract?.Content ?? "",
                    IsImportantContract = contract?.IsImportantContract ?? false,
                    ExplanationHtml = contract?.ExplanationHtml ?? "",
                    Status = a.Status,
                    StatusText = statusText,
                    IsProxyAgreement = a.IsProxyAgreement
                };
            }).ToList();

            ContractList.ItemsSource = _contracts;
        }

        private string GetStatusText(ContractStatus status) => status switch
        {
            ContractStatus.Pending => "未対応",
            ContractStatus.Agreed => "同意済み",
            ContractStatus.Rejected => "却下",
            ContractStatus.Disputed => "異議申し立て中",
            ContractStatus.Terminated => "打ち切り済み",
            _ => "不明"
        };

        private void ContractList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContractList.SelectedItem is ContractDisplayItem item)
            {
                _selectedContract = item;
                ShowContractDetail(item);
            }
        }

        private void ShowContractDetail(ContractDisplayItem item)
        {
            DetailPanel.Visibility = Visibility.Visible;
            NoSelectionText.Visibility = Visibility.Collapsed;

            ContractTitle.Text = item.ContractTitle;
            ContractContent.Text = item.ContractContent;
            StatusText.Text = item.StatusText;

            // Reset state
            _recordedAudio = null;
            _idDocumentBytes = null;
            IdDocumentFileNameText.Text = "未選択";

            // Important Contract Flow
            if (item.IsImportantContract && item.Status == ContractStatus.Pending)
            {
                ImportantContractSection.Visibility = Visibility.Visible;
                NormalContentSection.Visibility = Visibility.Collapsed;
                ActionButtons.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImportantContractSection.Visibility = Visibility.Collapsed;
                NormalContentSection.Visibility = Visibility.Visible;
                IdDocumentSection.Visibility = item.IsImportantContract ? Visibility.Visible : Visibility.Collapsed;
                ActionButtons.Visibility = Visibility.Visible;
            }

            // Set status badge color
            switch (item.Status)
            {
                case ContractStatus.Pending:
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(161, 98, 7));
                    break;
                case ContractStatus.Agreed:
                    if (item.IsProxyAgreement)
                    {
                        // 代理同意はオレンジ系で表示（不完全な同意であることを示す）
                        StatusBadge.Background = new SolidColorBrush(Color.FromRgb(254, 215, 170));
                        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(194, 65, 12));
                    }
                    else
                    {
                        StatusBadge.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                    }
                    break;
                case ContractStatus.Rejected:
                case ContractStatus.Terminated:
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                    break;
                case ContractStatus.Disputed:
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(254, 215, 170));
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(194, 65, 12));
                    break;
            }

            // Show/hide buttons based on status
            if (item.Status == ContractStatus.Pending)
            {
                AgreeButton.Visibility = Visibility.Visible;
                RejectButton.Visibility = Visibility.Visible;
                DisputeButton.Visibility = Visibility.Visible;
                TerminateButton.Visibility = Visibility.Collapsed;
            }
            else if (item.Status == ContractStatus.Agreed)
            {
                // ★ 代理同意の場合、消費者は拒否と解約（打ち切り）が可能
                if (item.IsProxyAgreement)
                {
                    AgreeButton.Visibility = Visibility.Collapsed;
                    RejectButton.Visibility = Visibility.Visible;  // 拒否可能
                    DisputeButton.Visibility = Visibility.Visible;
                    TerminateButton.Visibility = Visibility.Visible; // 解約可能
                }
                else
                {
                    AgreeButton.Visibility = Visibility.Collapsed;
                    RejectButton.Visibility = Visibility.Collapsed;
                    DisputeButton.Visibility = Visibility.Visible;
                    TerminateButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                AgreeButton.Visibility = Visibility.Collapsed;
                RejectButton.Visibility = Visibility.Collapsed;
                DisputeButton.Visibility = Visibility.Collapsed;
                TerminateButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Logout();
        }

        private void Agree_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContract == null) return;

            var dialog = new SignatureDialog();
            if (dialog.ShowDialog() == true)
            {
                string audioBase64 = "";
                string audioHash = "";
                string idDocBase64 = "";
                string idDocKey = "";

                if (_recordedAudio != null && _recordedAudio.Length > 0)
                {
                    audioBase64 = System.Convert.ToBase64String(_recordedAudio);
                    audioHash = CryptoService.ComputeSha256(_recordedAudio);
                }

                if (_idDocumentBytes != null && _idDocumentBytes.Length > 0)
                {
                    var (encData, encKey) = CryptoService.EncryptIdDocument(_idDocumentBytes);
                    idDocBase64 = encData;
                    idDocKey = encKey;
                }

                var userId = AuthService.CurrentUser?.Id ?? "";
                var (success, message) = ContractService.AgreeToContract(
                    _selectedContract.ContractId, 
                    userId,
                    dialog.SignatureName,
                    dialog.SignatureBirthDate,
                    dialog.SignatureImage,
                    audioBase64,
                    audioHash,
                    idDocBase64,
                    idDocKey);

                if (success)
                {
                    MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadContracts();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewExplanation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContract == null) return;

            var dialog = new ExplanationViewerDialog(_selectedContract.ExplanationHtml);
            if (dialog.ShowDialog() == true)
            {
                _recordedAudio = dialog.AudioData;
                
                ImportantContractSection.Visibility = Visibility.Collapsed;
                NormalContentSection.Visibility = Visibility.Visible;
                IdDocumentSection.Visibility = Visibility.Visible;
                
                // Refresh ActionButtons visibility based on status (which is Pending)
                ActionButtons.Visibility = Visibility.Visible;
                AgreeButton.Visibility = Visibility.Visible;
                RejectButton.Visibility = Visibility.Visible;
                DisputeButton.Visibility = Visibility.Visible;
                TerminateButton.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectIdDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif|All Files|*.*",
                Title = "本人確認書類を選択"
            };

            if (dialog.ShowDialog() == true)
            {
                _idDocumentBytes = System.IO.File.ReadAllBytes(dialog.FileName);
                IdDocumentFileNameText.Text = System.IO.Path.GetFileName(dialog.FileName);
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContract == null) return;

            // ★ 代理同意からの拒否の場合は特別なメッセージ
            string confirmMessage = _selectedContract.IsProxyAgreement
                ? "この契約は代理同意されています。\n消費者保護法に基づき、拒否しますか？"
                : "この契約書を却下しますか？";

            var result = MessageBox.Show(confirmMessage, "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var userId = AuthService.CurrentUser?.Id ?? "";
                
                // 代理同意の場合は、まず保留に戻してから拒否する
                if (_selectedContract.IsProxyAgreement)
                {
                    ContractService.ResetToPending(_selectedContract.ContractId, userId);
                }
                
                var (success, message) = ContractService.RejectContract(_selectedContract.ContractId, userId);

                if (success)
                {
                    MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadContracts();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Dispute_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContract == null) return;

            var dialog = new DisputeDialog();
            if (dialog.ShowDialog() == true)
            {
                var userId = AuthService.CurrentUser?.Id ?? "";
                var (success, message) = ContractService.DisputeContract(
                    _selectedContract.ContractId, 
                    userId, 
                    dialog.DisputeReason);

                if (success)
                {
                    MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadContracts();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Terminate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContract == null) return;

            var dialog = new DisputeDialog("契約打ち切り", "打ち切り理由を入力してください：");
            if (dialog.ShowDialog() == true)
            {
                var userId = AuthService.CurrentUser?.Id ?? "";
                var (success, message) = ContractService.TerminateContract(
                    _selectedContract.ContractId, 
                    userId, 
                    dialog.DisputeReason);

                if (success)
                {
                    MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadContracts();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class ContractDisplayItem
    {
        public ContractAssignment Assignment { get; set; } = null!;
        public string ContractId { get; set; } = "";
        public string ContractTitle { get; set; } = "";
        public string ContractContent { get; set; } = "";
        public bool IsImportantContract { get; set; } = false;
        public string ExplanationHtml { get; set; } = "";
        public ContractStatus Status { get; set; }
        public string StatusText { get; set; } = "";
        public bool IsProxyAgreement { get; set; } = false;
    }
}
