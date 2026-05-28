// FILE: Features/Admin/CourseOffering/Queries/GetAllCourseOfferings/GetAllCourseOfferingsQueryHandler.cs
// FIX: AppUser inherits IdentityUser (string key) NOT BaseEntity<string>,
//      so IUnitOfWork.GetRepository<AppUser, string>() violates the generic constraint.
//      Solution: inject UserManager<AppUser> to look up professor display names instead.
//
// IMPLEMENTATION FLOW:
// 1) Load CourseOfferings, Courses, Semesters, Professors.
// 2) For each offering, resolve the AppUser for professor name via UserManager.
// 3) Map to CourseOfferingDto list.
//
// DETAILED FLOW DIAGRAM:
//
//   [Request: Get All Offerings]
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Fetch Repositories   | <--- | Load: Offerings, Courses, Semesters,    |
//   | (UoW pattern)        |      | Professors                              |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Identity Resolution  | <--- | For each Professor, use UserManager     |
//   | (Prof. Name lookup)  |      | to find AppUser.Name (Identity Table)   |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | In-Memory Join       | <--- | Match Offering IDs to Course Codes,     |
//   | (DTO Mapping)        |      | Semester Names, and Professor Names     |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+
//   | Result Aggregation   | --- List<CourseOfferingDto>
//   +----------------------+
//            |
//            v
//   [200 OK: Admin Dashboard View]

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Queries.GetAllCourseOfferings;

public class GetAllCourseOfferingsQueryHandler(
    IUnitOfWork _uow,
    UserManager<AppUser> _userManager)
    : IRequestHandler<GetAllCourseOfferingsQuery, Result<List<CourseOfferingDto>>>
{
    public async Task<Result<List<CourseOfferingDto>>> Handle(
        GetAllCourseOfferingsQuery request, CancellationToken ct)
    {
        var offerings  = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().GetAllAsync();
        var courses    = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var semesters  = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>().GetAllAsync();
        var professors = await _uow.GetRepository<Professor, Guid>().GetAllAsync();

        var dtos = new List<CourseOfferingDto>();

        foreach (var o in offerings)
        {
            var course    = courses.FirstOrDefault(c => c.Id == o.CourseId);
            var semester  = semesters.FirstOrDefault(s => s.Id == o.SemesterId);
            var professor = professors.FirstOrDefault(p => p.Id == o.ProfessorId);

            // UserManager is the correct way to access AppUser — it bypasses the
            // BaseEntity<TKey> generic constraint that IUnitOfWork enforces.
            AppUser? profUser = professor is not null
                ? await _userManager.FindByIdAsync(professor.AppUserId)
                : null;

            dtos.Add(new CourseOfferingDto
            {
                Id            = o.Id,
                CourseCode    = course?.Code   ?? string.Empty,
                CourseName    = course?.Name   ?? string.Empty,
                SemesterName  = semester?.Name ?? string.Empty,
                ProfessorName = profUser?.Name ?? string.Empty
            });
        }

        return Result<List<CourseOfferingDto>>.Ok(dtos);
    }
}
