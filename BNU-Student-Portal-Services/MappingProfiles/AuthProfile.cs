using AutoMapper;
using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Services.MappingProfiles
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {

            CreateMap<RegisterStudentDto, Student>()
                .ForMember(dest => dest.CertificateType,
                    opt => opt.MapFrom(src => Enum.Parse<CertificateType>(src.CertificateType, ignoreCase: true)))
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore())  // set manually after CreateAsync
                .ForMember(dest => dest.AppUser, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<RegisterProfessorDto, Professor>()
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore())
                .ForMember(dest => dest.AppUser, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<RegisterTADto, TeachingAssistant>()
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore())
                .ForMember(dest => dest.AppUser, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
