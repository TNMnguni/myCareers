using myCareers.Core.Entities;
using System.Threading.Tasks;

namespace myCareers.Core.Interfaces
{
    public interface IRecruiterRepository
    {
        Task<Recruiter?> GetByUserIdAsync(int userId);
        
    }
}
