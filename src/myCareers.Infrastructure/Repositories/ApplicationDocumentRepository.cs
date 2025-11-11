using Microsoft.EntityFrameworkCore;
using myCareers.Core.Entities;
using myCareers.Core.Interfaces;
using myCareers.Infrastructure.Data;

namespace myCareers.Infrastructure.Repositories
{
    public class ApplicationDocumentRepository : IApplicationDocumentRepository
    {
        private readonly myCareersDbContext _context;

        public ApplicationDocumentRepository(myCareersDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationDocument> CreateAsync(ApplicationDocument document)
        {
            _context.ApplicationDocuments.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<ApplicationDocument?> GetByIdAsync(Guid fileId)
        {
            return await _context.ApplicationDocuments
                .Include(d => d.Applicant)
                .Include(d => d.JobApplication)
                .FirstOrDefaultAsync(d => d.FileId == fileId);
        }

        public async Task<List<ApplicationDocument>> GetByApplicantIdAsync(int applicantId)
        {
            return await _context.ApplicationDocuments
                .Where(d => d.ApplicantId == applicantId)
                .OrderByDescending(d => d.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<ApplicationDocument>> GetByApplicationIdAsync(int applicationId)
        {
            return await _context.ApplicationDocuments
                .Where(d => d.JobApplicationId == applicationId)
                .OrderBy(d => d.DocumentType)
                .ToListAsync();
        }

        public async Task<ApplicationDocument?> GetByApplicationAndTypeAsync(int applicationId, string documentType)
        {
            return await _context.ApplicationDocuments
                .FirstOrDefaultAsync(d => d.JobApplicationId == applicationId &&
                                        d.DocumentType == documentType);
        }

        public async Task DeleteAsync(Guid fileId)
        {
            var document = await _context.ApplicationDocuments.FindAsync(fileId);
            if (document != null)
            {
                _context.ApplicationDocuments.Remove(document);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasRequiredDocumentsAsync(int applicationId)
        {
            var documents = await GetByApplicationIdAsync(applicationId);

            // Check for required documents
            var hasResume = documents.Any(d => d.DocumentType.Equals("Resume", StringComparison.OrdinalIgnoreCase));
            var hasId = documents.Any(d => d.DocumentType.Equals("IdDocument", StringComparison.OrdinalIgnoreCase));
            var hasQualification = documents.Any(d => d.DocumentType.Equals("QualificationCertificate", StringComparison.OrdinalIgnoreCase));

            return hasResume && hasId && hasQualification;
        }

        public async Task<bool> LinkDocumentToApplicationAsync(Guid fileId, int applicationId)
        {
            var document = await _context.ApplicationDocuments.FindAsync(fileId);
            if (document == null) return false;

            document.JobApplicationId = applicationId;
            _context.ApplicationDocuments.Update(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlinkDocumentFromApplicationAsync(Guid fileId)
        {
            var document = await _context.ApplicationDocuments.FindAsync(fileId);
            if (document == null) return false;

            document.JobApplicationId = null;
            _context.ApplicationDocuments.Update(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ApplicationDocument?> UpdateAsync(ApplicationDocument document)
        {
            _context.ApplicationDocuments.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task UpdateApplicationIdAsync(Guid fileId, int applicationId)
        {
            var document = await _context.ApplicationDocuments.FindAsync(fileId);
            if (document != null)
            {
                document.JobApplicationId = applicationId;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ApplicationDocument>> GetOrphanedDocumentsByApplicantAsync(int applicantId)
        {
            return await _context.ApplicationDocuments
                .Where(d => d.ApplicantId == applicantId && d.JobApplicationId == null)
                .OrderByDescending(d => d.CreatedOn)
                .ToListAsync();
        }
    }
}