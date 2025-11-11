using myCareers.Application.Configuration;
using Microsoft.Extensions.Options;

namespace myCareers.Web.Extensions
{
    public static class DirectorySetup
    {
        public static void EnsureUploadDirectoriesExist(IServiceProvider serviceProvider)
        {
            var fileSettings = serviceProvider.GetRequiredService<IOptions<FileStorageSettings>>().Value;
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadPath = Path.Combine(basePath, fileSettings.UploadPath);

            var directories = new[]
            {
                Path.Combine(uploadPath, fileSettings.ApplicationsFolder),
                Path.Combine(uploadPath, fileSettings.Z83Folder),
                Path.Combine(uploadPath, fileSettings.ResumesFolder),
                Path.Combine(uploadPath, fileSettings.DocumentsFolder)
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }
    }
}