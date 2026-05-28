// FILE: Features/Admin/CourseOffering/Queries/GetAllCourseOfferings/GetAllCourseOfferingsQueryHandler.cs

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Queries.GetAllCourseOfferings;

public class GetAllCourseOfferingsQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetAllCourseOfferingsQuery, Result<List<CourseOfferingDto>>>
{
    public async Task<Result<List<CourseOfferingDto>>> Handle(
        GetAllCourseOfferingsQuery request, CancellationToken ct)
    {
        var offerings  = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().GetAllAsync();
        var courses    = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var semesters  = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>().GetAllAsync();
        var professors = await _uow.GetRepository<Professor, Guid>().GetAllAsync();
        var appUsers   = await _uow.GetRepository<AppUser, string>().GetAllAsync();

        var dtos = offerings.Select(o =>
        {
            var course    = courses.FirstOrDefault(c => c.Id == o.CourseId);
            var semester  = semesters.FirstOrDefault(s => s.Id == o.SemesterId);
            var professor = professors.FirstOrDefault(p => p.Id == o.ProfessorId);
            var profUser  = appUsers.FirstOrDefault(u => u.Id == professor?.AppUserId);

            return new CourseOfferingDto
            {
                Id            = o.Id,
                CourseCode    = course?.Code   ?? string.Empty,
                CourseName    = course?.Name   ?? string.Empty,
                SemesterName  = semester?.Name ?? string.Empty,
                ProfessorName = profUser?.Name ?? string.Empty
            };
        }).ToList();

        return Result<List<CourseOfferingDto>>.Ok(dtos);
    }
}
