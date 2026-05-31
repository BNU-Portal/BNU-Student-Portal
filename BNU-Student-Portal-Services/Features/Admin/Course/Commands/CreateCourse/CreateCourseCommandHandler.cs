// FILE: Features/Admin/Course/Commands/CreateCourse/CreateCourseCommandHandler.cs
// PURPOSE: Persists a new Course row.
//
// IMPLEMENTATION FLOW:
// 1) Check for duplicate course Code (case-insensitive).
//    - Fails with Error.BadRequest("Course.DuplicateCode") if already exists.
// 2) Create Course entity.
// 3) Add via UoW repository and SaveChanges.
// 4) Return CourseId to the caller.

using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Course.Commands.CreateCourse;

public class CreateCourseCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var existing = await _uow
            .GetRepository<BNU_Student_Portal_Domain.Entities.Courses.Course, Guid>()
            .GetAllAsync();

        var duplicate = existing
            .Any(c => string.Equals(c.Code, request.Code, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
            return Result<Guid>.Fail(
                Error.BadRequest("Course.DuplicateCode",
                    $"A course with code '{request.Code}' already exists."));

        var course = new BNU_Student_Portal_Domain.Entities.Courses.Course
        {
            Id          = Guid.NewGuid(),
            Code        = request.Code.ToUpperInvariant(),
            Name        = request.Name,
            CreditHours = request.CreditHours
        };

        await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.Course, Guid>().AddAsync(course);
        await _uow.SaveChangesAsync();

        return Result<Guid>.Ok(course.Id);
    }
}
