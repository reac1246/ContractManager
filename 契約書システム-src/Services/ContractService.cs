using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ContractManager.Models;

namespace ContractManager.Services
{
    public static class ContractService
    {
        // Contract CRUD
        public static List<Contract> GetAllContracts()
        {
            return DataStore.LoadContracts();
        }

        public static Contract? GetContractById(string id)
        {
            return DataStore.LoadContracts().FirstOrDefault(c => c.Id == id);
        }

        public static (bool success, string message, Contract? contract) CreateContract(string title, string content, string createdBy, bool isImportant = false, string explanationHtml = "")
        {
            var contracts = DataStore.LoadContracts();

            var contract = new Contract
            {
                Title = title,
                Content = content,
                CreatedBy = createdBy,
                ContentHash = ComputeSha256(content),
                IsImportantContract = isImportant,
                ExplanationHtml = explanationHtml
            };

            contracts.Add(contract);
            DataStore.SaveContracts(contracts);

            return (true, "契約書を作成しました", contract);
        }

        public static (bool success, string message) UpdateContract(string id, string? title = null, string? content = null, bool? isImportant = null, string? explanationHtml = null)
        {
            var contracts = DataStore.LoadContracts();
            var contract = contracts.FirstOrDefault(c => c.Id == id);

            if (contract == null)
            {
                return (false, "契約書が見つかりません");
            }

            // 本文が変更された場合のみ、ダイジェストを再計算し同意を撤回する
            bool contentChanged = content != null && content != contract.Content;

            if (title != null)
            {
                contract.Title = title;
            }

            if (content != null)
            {
                contract.Content = content;
                contract.ContentHash = ComputeSha256(content);
            }

            if (isImportant.HasValue)
            {
                contract.IsImportantContract = isImportant.Value;
            }

            if (explanationHtml != null)
            {
                contract.ExplanationHtml = explanationHtml;
            }

            contract.UpdatedAt = DateTime.Now;
            DataStore.SaveContracts(contracts);

            // ★ 契約書の本文が改変された場合、同意済みの紐づけを全て自動撤回する
            if (contentChanged)
            {
                RevokeAgreementsForContract(id);
            }

            return (true, contentChanged 
                ? "契約書を更新しました。内容が変更されたため、同意済みの契約は全て撤回されました。" 
                : "契約書を更新しました");
        }

        /// <summary>
        /// 契約書の内容変更に伴い、同意済み(Agreed)の全紐づけをPendingに戻す（自動撤回）
        /// ダイジェスト不一致による認証不可による拒否と撤回
        /// </summary>
        private static void RevokeAgreementsForContract(string contractId)
        {
            var assignments = DataStore.LoadAssignments();
            bool changed = false;

            foreach (var assignment in assignments.Where(a => a.ContractId == contractId && a.Status == ContractStatus.Agreed))
            {
                assignment.Status = ContractStatus.Pending;
                assignment.RespondedAt = null;
                assignment.SignatureName = "";
                assignment.SignatureImage = "";
                assignment.SignatureBirthDate = null;
                assignment.AgreedContentHash = "";
                assignment.IsProxyAgreement = false;
                changed = true;
            }

            if (changed)
            {
                DataStore.SaveAssignments(assignments);
            }
        }

        public static (bool success, string message) DeleteContract(string id)
        {
            var contracts = DataStore.LoadContracts();
            var contract = contracts.FirstOrDefault(c => c.Id == id);

            if (contract == null)
            {
                return (false, "契約書が見つかりません");
            }

            // Also delete all assignments for this contract
            var assignments = DataStore.LoadAssignments();
            assignments.RemoveAll(a => a.ContractId == id);
            DataStore.SaveAssignments(assignments);

            contracts.Remove(contract);
            DataStore.SaveContracts(contracts);

            return (true, "契約書を削除しました");
        }

        // Assignment operations
        public static List<ContractAssignment> GetAllAssignments()
        {
            return DataStore.LoadAssignments();
        }

        public static List<ContractAssignment> GetAssignmentsForUser(string userId)
        {
            return DataStore.LoadAssignments().Where(a => a.UserId == userId).ToList();
        }

        public static List<ContractAssignment> GetAssignmentsForContract(string contractId)
        {
            return DataStore.LoadAssignments().Where(a => a.ContractId == contractId).ToList();
        }

        public static ContractAssignment? GetAssignment(string contractId, string userId)
        {
            return DataStore.LoadAssignments()
                .FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);
        }

        public static (bool success, string message) AssignContract(string contractId, string userId)
        {
            var contracts = DataStore.LoadContracts();
            var users = DataStore.LoadUsers();
            var assignments = DataStore.LoadAssignments();

            if (!contracts.Any(c => c.Id == contractId))
            {
                return (false, "契約書が見つかりません");
            }

            if (!users.Any(u => u.Id == userId))
            {
                return (false, "ユーザーが見つかりません");
            }

            if (assignments.Any(a => a.ContractId == contractId && a.UserId == userId))
            {
                return (false, "この契約書は既にこのユーザーに紐づけられています");
            }

            var assignment = new ContractAssignment
            {
                ContractId = contractId,
                UserId = userId,
                Status = ContractStatus.Pending
            };

            assignments.Add(assignment);
            DataStore.SaveAssignments(assignments);

            return (true, "契約書を紐づけました");
        }

        public static (bool success, string message) UnassignContract(string contractId, string userId)
        {
            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "紐づけが見つかりません");
            }

            assignments.Remove(assignment);
            DataStore.SaveAssignments(assignments);

            return (true, "紐づけを解除しました");
        }

        // User actions on assignments
        public static (bool success, string message) AgreeToContract(string contractId, string userId, string signatureName, DateTime signatureBirthDate, string signatureImage, string audioBase64 = "", string audioHash = "", string idDocEncryptedBase64 = "", string idDocEncryptedKey = "")
        {
            var contracts = DataStore.LoadContracts();
            var contract = contracts.FirstOrDefault(c => c.Id == contractId);
            
            if (contract == null)
            {
                return (false, "契約書本体が見つかりません");
            }

            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "契約書が見つかりません");
            }

            if (assignment.Status != ContractStatus.Pending)
            {
                return (false, "この契約書は既に処理されています");
            }

            assignment.Status = ContractStatus.Agreed;
            assignment.RespondedAt = DateTime.Now;
            assignment.SignatureName = signatureName;
            assignment.SignatureBirthDate = signatureBirthDate;
            assignment.SignatureImage = signatureImage;
            assignment.AgreedContentHash = contract.ContentHash;
            assignment.IsProxyAgreement = false;
            assignment.AudioRecordingBase64 = audioBase64;
            assignment.AudioHash = audioHash;
            assignment.IdDocumentEncryptedBase64 = idDocEncryptedBase64;
            assignment.IdDocumentEncryptedAesKey = idDocEncryptedKey;

            DataStore.SaveAssignments(assignments);
            return (true, "契約書に同意しました");
        }

        /// <summary>
        /// 代理同意: 電話口などでの代理同意。署名なし。
        /// 不正防止の観点から、自書署名ができないため、消費者は後から拒否・解約が可能。
        /// </summary>
        public static (bool success, string message) ProxyAgreeToContract(string contractId, string userId)
        {
            var contracts = DataStore.LoadContracts();
            var contract = contracts.FirstOrDefault(c => c.Id == contractId);
            
            if (contract == null)
            {
                return (false, "契約書本体が見つかりません");
            }

            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "紐づけが見つかりません");
            }

            if (assignment.Status != ContractStatus.Pending)
            {
                return (false, "未対応の契約のみ代理同意できます");
            }

            assignment.Status = ContractStatus.Agreed;
            assignment.RespondedAt = DateTime.Now;
            assignment.SignatureName = ""; // 代理同意では署名なし
            assignment.SignatureBirthDate = null;
            assignment.SignatureImage = ""; // 代理同意では署名画像なし
            assignment.AgreedContentHash = contract.ContentHash;
            assignment.IsProxyAgreement = true; // 代理同意フラグ

            DataStore.SaveAssignments(assignments);
            return (true, "代理同意を行いました（署名なし）。消費者は後から拒否・解約が可能です。");
        }

        public static (bool success, string message) RejectContract(string contractId, string userId)
        {
            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "契約書が見つかりません");
            }

            if (assignment.Status != ContractStatus.Pending)
            {
                return (false, "この契約書は既に処理されています");
            }

            assignment.Status = ContractStatus.Rejected;
            assignment.RespondedAt = DateTime.Now;

            DataStore.SaveAssignments(assignments);
            return (true, "契約書を却下しました");
        }

        public static (bool success, string message) DisputeContract(string contractId, string userId, string reason)
        {
            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "契約書が見つかりません");
            }

            assignment.Status = ContractStatus.Disputed;
            assignment.RespondedAt = DateTime.Now;
            assignment.DisputeReason = reason;

            DataStore.SaveAssignments(assignments);
            return (true, "異議申し立てを送信しました");
        }

        public static (bool success, string message) TerminateContract(string contractId, string userId, string reason)
        {
            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "契約書が見つかりません");
            }

            if (assignment.Status != ContractStatus.Agreed)
            {
                return (false, "同意済みの契約のみ打ち切りできます");
            }

            assignment.Status = ContractStatus.Terminated;
            assignment.RespondedAt = DateTime.Now;
            assignment.TerminationReason = reason;

            DataStore.SaveAssignments(assignments);
            return (true, "契約を打ち切りました");
        }

        // Admin dispute resolution
        // ★ 修正: 拒否(Rejected)から直接「同意済み」にはできないようにする
        //   異議申し立て(Disputed)と打ち切り(Terminated)のみ解決可能
        public static (bool success, string message) ResolveDispute(string contractId, string userId)
        {
            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "契約書が見つかりません");
            }

            // 異議申し立て・打ち切りのみ解決可能
            if (assignment.Status != ContractStatus.Disputed && assignment.Status != ContractStatus.Terminated)
            {
                return (false, "異議申し立てまたは打ち切りの契約のみ解決できます");
            }

            // ★ 修正: 勝手に「同意済み」にせず、「未対応(Pending)」に戻してユーザーに再署名を求める
            assignment.Status = ContractStatus.Pending;
            assignment.RespondedAt = null;
            assignment.DisputeReason = "";
            assignment.TerminationReason = "";
            assignment.SignatureName = "";
            assignment.SignatureImage = "";
            assignment.SignatureBirthDate = null;
            assignment.AgreedContentHash = "";
            assignment.IsProxyAgreement = false;

            DataStore.SaveAssignments(assignments);
            return (true, "解決済みにし、未対応（再署名待ち）に戻しました");
        }

        public static (bool success, string message) ResetToPending(string contractId, string userId)
        {
            var assignments = DataStore.LoadAssignments();
            var assignment = assignments.FirstOrDefault(a => a.ContractId == contractId && a.UserId == userId);

            if (assignment == null)
            {
                return (false, "契約書が見つかりません");
            }

            assignment.Status = ContractStatus.Pending;
            assignment.RespondedAt = null;
            assignment.DisputeReason = "";
            assignment.TerminationReason = "";
            assignment.SignatureName = "";
            assignment.SignatureImage = "";
            assignment.SignatureBirthDate = null;
            assignment.AgreedContentHash = "";
            assignment.IsProxyAgreement = false;

            DataStore.SaveAssignments(assignments);
            return (true, "未対応に戻しました");
        }

        /// <summary>
        /// 契約書のダイジェスト（SHA256）の整合性を検証する
        /// </summary>
        public static bool VerifyContractIntegrity(Contract contract)
        {
            return contract.ContentHash == ComputeSha256(contract.Content);
        }

        /// <summary>
        /// 署名時点のダイジェストと現在のダイジェストが一致するかを検証する
        /// </summary>
        public static bool VerifyAgreementIntegrity(ContractAssignment assignment, Contract contract)
        {
            if (string.IsNullOrEmpty(assignment.AgreedContentHash))
                return false;
            return assignment.AgreedContentHash == contract.ContentHash;
        }

        private static string ComputeSha256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData ?? ""));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
