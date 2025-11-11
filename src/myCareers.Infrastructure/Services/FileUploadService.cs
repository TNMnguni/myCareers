using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using myCareers.Application.Configuration;
using myCareers.Application.Interfaces;

namespace myCareers.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly FileStorageSettings _fileSettings;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _basePath;
        private readonly string[] _allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

        public FileUploadService(
            IOptions<FileStorageSettings> fileSettings,
            ILogger<FileUploadService> logger)
        {
            _fileSettings = fileSettings.Value;
            _logger = logger;

            // Get the base path from the current directory (will be wwwroot in web app)
            _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        Errors = new List<string> { "No file was uploaded" }
                    };
                }

                if (!IsValidFileSize(file, _fileSettings.MaxFileSizeBytes))
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        Errors = new List<string> { $"File size exceeds maximum limit of {_fileSettings.MaxFileSizeBytes / (1024 * 1024)}MB" }
                    };
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!IsValidFileType(file, _fileSettings.AllowedExtensions))
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        Errors = new List<string> { $"File type {extension} is not allowed. Allowed types: {string.Join(", ", _fileSettings.AllowedExtensions)}" }
                    };
                }

                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadFolder = Path.Combine(_basePath, _fileSettings.UploadPath, folder);

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"{_fileSettings.BaseUrl}/{folder}/{fileName}";

                _logger.LogInformation("File uploaded successfully: {FileName}", fileName);

                return new FileUploadResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = fileName,
                    FileUrl = fileUrl,
                    FileSize = file.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return new FileUploadResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while uploading the file" }
                };
            }
        }

        public async Task<List<FileUploadResult>> UploadMultipleFilesAsync(List<IFormFile> files, string folder)
        {
            var results = new List<FileUploadResult>();

            foreach (var file in files)
            {
                var result = await UploadFileAsync(file, folder);
                results.Add(result);
            }

            return results;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_basePath, filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public bool IsValidFileType(IFormFile file, string[] allowedExtensions)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        public bool IsValidFileSize(IFormFile file, long maxSizeInBytes)
        {
            return file.Length <= maxSizeInBytes;
        }
    }
}