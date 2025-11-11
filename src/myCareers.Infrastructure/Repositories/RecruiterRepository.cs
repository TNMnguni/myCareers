using Microsoft.EntityFrameworkCore;
using myCareers.Core.Entities;
using myCareers.Core.Interfaces;
using myCareers.Infrastructure.Data;
using System.Threading.Tasks;

namespace myCareers.Infrastructure.Repositories
{
    public class RecruiterRepository : IRecruiterRepository
    {
        private readonly myCareersDbContext _context;

        public RecruiterRepository(myCareersDbContext context)
        {
            _context = context;
        }

        public async Task<Recruiter?> GetByUserIdAsync(int userId)
        {
            return await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == userId);
        }

        public async Task<Recruiter> CreateAsync(Recruiter recruiter)
        {
            _context.Recruiters.Add(recruiter);
            await _context.SaveChangesAsync();
            return recruiter;
        }
    }
}
