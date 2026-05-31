// FILE: Features/Admin/Course/Queries/GetAllCourses/GetAllCoursesQueryHandler.cs
// PURPOSE: Fetch all Course rows, sort by Code asc, map to CourseDto.

using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Course.Queries.GetAllCourses;

public class GetAllCoursesQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetAllCoursesQuery, Result<List<CourseDto>>>
{
    public async Task<Result<List<CourseDto>>> Handle(
        GetAllCoursesQuery request, CancellationToken ct)
    {
        var courses = await _uow
            .GetRepository<BNU_Student_Portal_Domain.Entities.Courses.Course, Guid>()
            .GetAllAsync();

        var dtos = courses
            .OrderBy(c => c.Code)
            .Select(c => new CourseDto
            {
                Id          = c.Id,
                Code        = c.Code,
                Name        = c.Name,
                CreditHours = c.CreditHours
            })
            .ToList();

        return Result<List<CourseDto>>.Ok(dtos);
    }
}
