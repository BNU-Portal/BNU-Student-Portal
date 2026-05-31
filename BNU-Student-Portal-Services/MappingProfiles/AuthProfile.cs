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
                    opt => opt.MapFrom(src => ParseCertificateType(src.CertificateType)))
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore())
                .ForMember(dest => dest.AppUser,   opt => opt.Ignore())
                .ForMember(dest => dest.Id,        opt => opt.Ignore());

            CreateMap<RegisterProfessorDto, Professor>()
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore())
                .ForMember(dest => dest.AppUser,   opt => opt.Ignore())
                .ForMember(dest => dest.Id,        opt => opt.Ignore());

            CreateMap<RegisterTADto, TeachingAssistant>()
                .ForMember(dest => dest.AppUserId, opt => opt.Ignore())
                .ForMember(dest => dest.AppUser,   opt => opt.Ignore())
                .ForMember(dest => dest.Id,        opt => opt.Ignore());
        }

        /// <summary>
        /// Accepts all of these (case-insensitive, spaces/underscores ignored):
        ///   "Thanaweya Amma" | "ThanaweyaAama" | "thanaweya_amma"
        ///   "IGCSE"
        ///   "IB"
        ///   "American High School Diploma" | "AmericanHighSchoolDiploma"
        ///   "Other"
        /// </summary>
        private static CertificateType ParseCertificateType(string value)
        {
            var normalized = value.Replace(" ", "").Replace("_", "").ToLowerInvariant();

            return normalized switch
            {
                "thanaweyaaama" or "thanaweyaamma" => CertificateType.ThanaweyaAama,
                "igcse"                            => CertificateType.IGCSE,
                "ib"                               => CertificateType.IB,
                "americanhighschooldiploma"        => CertificateType.AmericanHighSchoolDiploma,
                "other"                            => CertificateType.Other,
                _                                  => Enum.Parse<CertificateType>(value, ignoreCase: true)
            };
        }
    }
}
