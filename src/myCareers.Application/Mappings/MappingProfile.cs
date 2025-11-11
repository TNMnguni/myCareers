using AutoMapper;
using myCareers.Application.DTOs;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.DTOs.JobApplication;
using myCareers.Application.DTOs.JobPosting;
using myCareers.Core.Entities;


namespace myCareers.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.Role.ToString()));

            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.ToLower()))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsEmailConfirmed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.LastLoginDate, opt => opt.Ignore())
                .ForMember(dest => dest.Recruiter, opt => opt.Ignore())
                .ForMember(dest => dest.Applicant, opt => opt.Ignore());

            // JobApplication mappings
            CreateMap<JobApplication, JobApplicationDto>()
                .ForMember(dest => dest.ApplicantName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.ApplicantEmail,
                    opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber,
                    opt => opt.MapFrom(src => src.PhoneNumber))
                // Map document URLs - these will be populated separately in the service
                .ForMember(dest => dest.ResumeUrl, opt => opt.Ignore())
                .ForMember(dest => dest.IdDocumentUrl, opt => opt.Ignore())
                .ForMember(dest => dest.QualificationCertificateUrl, opt => opt.Ignore())
                .ForMember(dest => dest.TranscriptUrl, opt => opt.Ignore())
                .ForMember(dest => dest.Z83FormUrl, opt => opt.Ignore());

            // JobPosting mappings
            CreateMap<JobPosting, JobPostingDto>()
                .ForMember(dest => dest.RecruiterName, opt => opt.MapFrom(src => $"{src.Recruiter.User.FirstName} {src.Recruiter.User.LastName}"))
                .ForMember(dest => dest.RecruiterEmail, opt => opt.MapFrom(src => src.Recruiter.User.Email));
           
            CreateMap<UpdateJobPostingDto, JobPosting>();

            CreateMap<User, UserDto>().ForMember(dest => dest.RecruiterProfile, opt => opt.MapFrom(src => src.Recruiter)).ForMember(dest => dest.ApplicantProfile, opt => opt.MapFrom(src => src.Applicant));

            CreateMap<Recruiter, RecruiterProfileDto>();
            CreateMap<Applicant, ApplicantProfileDto>();


        }
    }
}
