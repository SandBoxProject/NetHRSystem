using System;

namespace NetHRSystem.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsPublic { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsPublic { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Tags { get; set; } = string.Empty;
    }

    public class UpdateDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Tags { get; set; } = string.Empty;
    }
}
