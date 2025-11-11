namespace myCareers.Application.Configuration
{
    public class FileStorageSettings
    {
        public string UploadPath { get; set; } = "uploads";
        public string ApplicationsFolder { get; set; } = "applications";
        public string Z83Folder { get; set; } = "z83";
        public string ResumesFolder { get; set; } = "resumes";
        public string DocumentsFolder { get; set; } = "documents";
        public long MaxFileSizeBytes { get; set; } = 5242880; // 5MB
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png" };
        public string BaseUrl { get; set; } = "/uploads";
    }
}