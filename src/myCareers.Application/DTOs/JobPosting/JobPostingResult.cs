

namespace myCareers.Application.DTOs.JobPosting
{
    public class JobPostingResult
    {
        public bool Success { get; set; }
        public JobPostingDto? JobPosting { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
