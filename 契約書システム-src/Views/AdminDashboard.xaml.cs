using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ContractManager.Models;
using ContractManager.Services;

namespace ContractManager.Views
{
    public partial class AdminDashboard : UserControl
    {
        private readonly MainWindow _mainWindow;

        public AdminDashboard(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            WelcomeText.Text = $"ようこそ、{AuthService.CurrentUser?.Username} さん";
            
            LoadData();
        }

        private void LoadData()
        {
            // Load contracts
            ContractGrid.ItemsSource = ContractService.GetAllContracts();
            AssignContractList.ItemsSource = ContractService.GetAllContracts();
            
            // Load users (non-admin only)
            var users = UserService.GetNonAdminUsers();
            UserGrid.ItemsSource = users;
            AssignUserList.ItemsSource = users;
            
            // Load assignments with display info
            var assignments = ContractService.GetAllAssignments();
            var contracts = ContractService.GetAllContracts();
            var allUsers = UserService.GetAllUsers();
            
            var assignmentDisplay = assignments.Select(a => new
            {
                a.Id,
                a.ContractId,
                a.UserId,
                ContractTitle = contracts.FirstOrDefault(c => c.Id == a.ContractId)?.Title ?? "不明",
                Username = allUsers.FirstOrDefault(u => u.Id == a.UserId)?.Username ?? "不明",
                StatusText = GetStatusText(a.Status),
                a.AssignedAt,
                a.RespondedAt
            }).ToList();
            
            AssignmentGrid.ItemsSource = assignmentDisplay;
            
            // Load disputes
            LoadDisputes();
            
            // Load completed contracts
            LoadCompleted();
        }

        private string GetStatusText(ContractStatus status) => status switch
        {
            ContractStatus.Pending => "未対応",
            ContractStatus.Agreed => "同意済み",
            ContractStatus.Rejected => "却下",
            ContractStatus.Disputed => "異議申し立て",
            ContractStatus.Terminated => "打ち切り",
            _ => "不明"
        };

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Logout();
        }

        // Contract Management
        private void NewContract_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContractEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                ContractService.CreateContract(dialog.ContractTitle, dialog.ContractContent, AuthService.CurrentUser?.Id ?? "", dialog.IsImportantContract, dialog.ExplanationHtml);
                LoadData();
            }
        }

        private void EditContract_Click(object sender, RoutedEventArgs e)
        {
            if (ContractGrid.SelectedItem is Contract contract)
            {
                var dialog = new ContractEditorDialog(contract);
                if (dialog.ShowDialog() == true)
                {
                    var (success, message) = ContractService.UpdateContract(contract.Id, dialog.ContractTitle, dialog.ContractContent, dialog.IsImportantContract, dialog.ExplanationHtml);
                    MessageBox.Show(message, success ? "成功" : "エラー", MessageBoxButton.OK, 
                        success ? MessageBoxImage.Information : MessageBoxImage.Error);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("編集する契約書を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteContract_Click(object sender, RoutedEventArgs e)
        {
            if (ContractGrid.SelectedItem is Contract contract)
            {
                var result = MessageBox.Show($"契約書「{contract.Title}」を削除しますか？\n関連する紐づけも全て削除されます。", 
                    "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    ContractService.DeleteContract(contract.Id);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("削除する契約書を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // User Management
        private void NewUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                var (success, message) = UserService.CreateUser(dialog.Username, dialog.Password, dialog.NameEnglish, dialog.BirthDate);
                if (!success)
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                LoadData();
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UserGrid.SelectedItem is User user)
            {
                var dialog = new UserEditorDialog(user);
                if (dialog.ShowDialog() == true)
                {
                    string? password = string.IsNullOrEmpty(dialog.Password) ? null : dialog.Password;
                    var (success, message) = UserService.UpdateUser(user.Id, dialog.Username, password, dialog.NameEnglish, dialog.BirthDate);
                    if (!success)
                    {
                        MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("編集するユーザーを選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UserGrid.SelectedItem is User user)
            {
                var result = MessageBox.Show($"ユーザー「{user.Username}」を削除しますか？\n関連する紐づけも全て削除されます。", 
                    "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    UserService.DeleteUser(user.Id);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("削除するユーザーを選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Assignment Management
        private void AssignContract_Click(object sender, RoutedEventArgs e)
        {
            if (AssignContractList.SelectedItem is Contract contract && AssignUserList.SelectedItem is User user)
            {
                var (success, message) = ContractService.AssignContract(contract.Id, user.Id);
                if (success)
                {
                    MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("契約書とユーザーを両方選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UnassignContract_Click(object sender, RoutedEventArgs e)
        {
            if (AssignContractList.SelectedItem is Contract contract && AssignUserList.SelectedItem is User user)
            {
                var (success, message) = ContractService.UnassignContract(contract.Id, user.Id);
                if (success)
                {
                    MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("契約書とユーザーを両方選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Dispute Management
        private void LoadDisputes()
        {
            var assignments = ContractService.GetAllAssignments();
            var contracts = ContractService.GetAllContracts();
            var allUsers = UserService.GetAllUsers();

            // Filter to only show disputed, rejected, or terminated contracts
            var disputeDisplay = assignments
                .Where(a => a.Status == ContractStatus.Disputed || 
                           a.Status == ContractStatus.Rejected || 
                           a.Status == ContractStatus.Terminated)
                .Select(a => new
                {
                    a.Id,
                    a.ContractId,
                    a.UserId,
                    ContractTitle = contracts.FirstOrDefault(c => c.Id == a.ContractId)?.Title ?? "不明",
                    Username = allUsers.FirstOrDefault(u => u.Id == a.UserId)?.Username ?? "不明",
                    StatusText = GetStatusText(a.Status),
                    Reason = a.Status == ContractStatus.Disputed ? a.DisputeReason : 
                             a.Status == ContractStatus.Terminated ? a.TerminationReason : "却下",
                    a.RespondedAt,
                    a.Status
                }).ToList();

            DisputeGrid.ItemsSource = disputeDisplay;
        }

        private void ResolveDispute_Click(object sender, RoutedEventArgs e)
        {
            var selected = DisputeGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("解決する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            string contractId = item.ContractId;
            string userId = item.UserId;
            ContractStatus status = item.Status;

            // ★ 拒否された契約に対して「解決済みにする」を押した場合は、警告を出してブロック
            if (status == ContractStatus.Rejected)
            {
                MessageBox.Show("却下された契約を直接「同意済み」にすることはできません。\n\n「保留に戻す」を使用して、ユーザーに再度確認してもらってください。",
                    "操作不可", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("この契約を「未対応（再署名待ち）」に戻して解決済みにしますか？\n\n※ 異議申し立てまたは打ち切りの契約のみ解決可能です。", 
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var (success, message) = ContractService.ResolveDispute(contractId, userId);
                if (success)
                {
                    MessageBox.Show("解決済みに変更しました", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ★ 代理同意（電話対応）
        private void ProxyAgree_Click(object sender, RoutedEventArgs e)
        {
            var selected = DisputeGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("代理同意を行う項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            string contractId = item.ContractId;
            string userId = item.UserId;
            ContractStatus status = item.Status;

            // まず保留に戻してから代理同意を行う
            var confirmResult = MessageBox.Show(
                "代理同意を行いますか？\n\n" +
                "【注意】代理同意には以下の制約があります：\n" +
                "・自書署名ができません\n" +
                "・そのため契約書同意としては不十分です\n" +
                "・消費者は後から拒否と解約が行えます\n" +
                "（民法・刑法・消費者保護法に基づく）\n\n" +
                "続行しますか？",
                "代理同意の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmResult == MessageBoxResult.Yes)
            {
                // まず保留に戻す
                ContractService.ResetToPending(contractId, userId);
                
                // 代理同意を実行
                var (success, message) = ContractService.ProxyAgreeToContract(contractId, userId);
                if (success)
                {
                    MessageBox.Show(message, "代理同意完了", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetToPending_Click(object sender, RoutedEventArgs e)
        {
            var selected = DisputeGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("リセットする項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            string contractId = item.ContractId;
            string userId = item.UserId;

            var result = MessageBox.Show("この契約を「未対応」に戻しますか？\nユーザーに再度確認してもらえるようになります。", 
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var (success, message) = ContractService.ResetToPending(contractId, userId);
                if (success)
                {
                    MessageBox.Show("未対応に戻しました", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewDisputeDetail_Click(object sender, RoutedEventArgs e)
        {
            var selected = DisputeGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("詳細を表示する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            string reason = item.Reason;
            string status = item.StatusText;
            string contractTitle = item.ContractTitle;
            string username = item.Username;

            MessageBox.Show($"契約書: {contractTitle}\nユーザー: {username}\nステータス: {status}\n\n理由:\n{reason}", 
                "詳細", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Completed Contracts Management
        private void LoadCompleted()
        {
            var assignments = ContractService.GetAllAssignments();
            var contracts = ContractService.GetAllContracts();
            var allUsers = UserService.GetAllUsers();

            var completedDisplay = assignments
                .Where(a => a.Status == ContractStatus.Agreed)
                .Select(a => new
                {
                    Assignment = a,
                    a.Id,
                    a.ContractId,
                    a.UserId,
                    ContractTitle = contracts.FirstOrDefault(c => c.Id == a.ContractId)?.Title ?? "不明",
                    Username = allUsers.FirstOrDefault(u => u.Id == a.UserId)?.Username ?? "不明",
                    a.SignatureName,
                    a.SignatureBirthDate,
                    a.SignatureImage,
                    a.RespondedAt,
                    a.IsProxyAgreement,
                    AgreementType = a.IsProxyAgreement ? "代理同意（署名なし）" : "本人署名"
                }).ToList();

            CompletedGrid.ItemsSource = completedDisplay;
        }

        private void ViewSignature_Click(object sender, RoutedEventArgs e)
        {
            var selected = CompletedGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("署名を表示する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            string signatureImage = item.SignatureImage;
            string signatureName = item.SignatureName;
            bool isProxy = item.IsProxyAgreement;

            if (isProxy)
            {
                MessageBox.Show("この契約は代理同意のため、署名データがありません。\n\n消費者は後から拒否・解約が可能です。", 
                    "代理同意", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(signatureImage))
            {
                MessageBox.Show("署名データがありません", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show signature in a new window
            var dialog = new SignatureViewerDialog(signatureName, signatureImage);
            dialog.ShowDialog();
        }

        private void ViewVerificationData_Click(object sender, RoutedEventArgs e)
        {
            var selected = CompletedGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("検証データを表示する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            ContractAssignment assignment = item.Assignment;

            var dialog = new VerificationDetailsDialog(assignment);
            dialog.ShowDialog();
        }

        private void ViewCompletedDetail_Click(object sender, RoutedEventArgs e)
        {
            var selected = CompletedGrid.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("詳細を表示する項目を選択してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dynamic item = selected;
            string contractTitle = item.ContractTitle;
            string username = item.Username;
            string signatureName = item.SignatureName;
            DateTime? birthDate = item.SignatureBirthDate;
            DateTime? respondedAt = item.RespondedAt;
            bool isProxy = item.IsProxyAgreement;

            string birthDateStr = birthDate?.ToString("yyyy/MM/dd") ?? "なし";
            string respondedAtStr = respondedAt?.ToString("yyyy/MM/dd HH:mm") ?? "不明";
            string agreementType = isProxy ? "代理同意（署名なし）" : "本人署名";

            string proxyWarning = isProxy 
                ? "\n\n⚠ この契約は代理同意のため、署名がありません。\n消費者は後から拒否・解約が可能です。" 
                : "";

            MessageBox.Show($"契約書: {contractTitle}\nユーザー: {username}\n\n同意種別: {agreementType}\n署名氏名: {(string.IsNullOrEmpty(signatureName) ? "なし" : signatureName)}\n生年月日: {birthDateStr}\n締結日時: {respondedAtStr}{proxyWarning}", 
                "締結済み契約詳細", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
