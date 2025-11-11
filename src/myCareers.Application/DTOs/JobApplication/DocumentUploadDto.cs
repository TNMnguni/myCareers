using Microsoft.AspNetCore.Http;

namespace myCareers.Application.DTOs.JobApplication
{
    public class DocumentUploadDto
    {
        public IFormFile File { get; set; } = null!;
        public string DocumentType { get; set; } = string.Empty;
        public int ApplicantId { get; set; }
        public int? ApplicationId { get; set; }
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public Guid FileId { get; set; }
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public long FileSize { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}