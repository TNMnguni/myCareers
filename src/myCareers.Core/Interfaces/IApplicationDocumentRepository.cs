using myCareers.Core.Entities;

namespace myCareers.Core.Interfaces
{
    public interface IApplicationDocumentRepository
    {
        Task<ApplicationDocument> CreateAsync(ApplicationDocument document);
        Task<ApplicationDocument?> GetByIdAsync(Guid fileId);
        Task<List<ApplicationDocument>> GetByApplicantIdAsync(int applicantId);
        Task<List<ApplicationDocument>> GetByApplicationIdAsync(int applicationId);
        Task<ApplicationDocument?> GetByApplicationAndTypeAsync(int applicationId, string documentType);
        Task DeleteAsync(Guid fileId);
        Task<bool> HasRequiredDocumentsAsync(int applicationId);

        Task<bool> LinkDocumentToApplicationAsync(Guid fileId, int applicationId);
        Task<bool> UnlinkDocumentFromApplicationAsync(Guid fileId);
        Task<ApplicationDocument?> UpdateAsync(ApplicationDocument document);

        Task UpdateApplicationIdAsync(Guid fileId, int applicationId);
        Task<IEnumerable<ApplicationDocument>> GetOrphanedDocumentsByApplicantAsync(int applicantId);

    }
}