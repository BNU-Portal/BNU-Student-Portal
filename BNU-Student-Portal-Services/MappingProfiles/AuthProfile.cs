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
            CreateMap<RegisterStudentDto, Student>().ReverseMap();
            CreateMap<RegisterProfessorDto, Professor>().ReverseMap();
            CreateMap<RegisterTADto, TeachingAssistant>().ReverseMap();
        }
    }
}
