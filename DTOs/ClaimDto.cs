using System;

namespace NetHRSystem.DTOs
{
    public class ClaimDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid ClaimTypeId { get; set; }
        public string ClaimTypeName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ClaimDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Comments { get; set; }
        public string? ReceiptUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateClaimDto
    {
        public Guid ClaimTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ClaimDate { get; set; }
        public string? ReceiptUrl { get; set; }
    }

    public class ApproveClaimDto
    {
        public bool Approved { get; set; }
        public string? Comments { get; set; }
    }

    public class ClaimTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MaximumAmount { get; set; }
        public bool RequiresReceipt { get; set; }
        public bool RequiresApproval { get; set; }
    }

    public class ClaimSummaryDto
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public int ApprovedClaims { get; set; }
        public decimal ApprovedAmount { get; set; }
        public int PendingClaims { get; set; }
        public decimal PendingAmount { get; set; }
        public int RejectedClaims { get; set; }
        public decimal RejectedAmount { get; set; }
    }
}
