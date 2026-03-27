using AutoMapper;
using LifeLink_V2.DTOs.Auth;
using LifeLink_V2.Models;

namespace LifeLink_V2.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterPatientDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.ToLower()))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<User, UserInfoDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.RoleName))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.Patient != null ? src.Patient.PatientId : (int?)null))
                .ForMember(dest => dest.ProviderId, opt => opt.MapFrom(src => src.Provider != null ? src.Provider.ProviderId : (int?)null))
                .ForMember(dest => dest.ProviderName, opt => opt.MapFrom(src => src.Provider != null ? src.Provider.ProviderName : null));
        }
    }
}