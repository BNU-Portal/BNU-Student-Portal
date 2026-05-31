// FILE: Features/Admin/CourseOffering/Commands/CreateCourseOffering/CreateCourseOfferingCommandHandler.cs
// PURPOSE: Validates all FKs exist then persists a new CourseOffering row.
//
// IMPLEMENTATION FLOW:
// 1) Validate Course exists by CourseId.
// 2) Validate Semester exists by SemesterId.
// 3) Resolve Professor by AppUserId (NOT Professor.Id). Return 404 if not found.
// 4) Ensure no duplicate (CourseId + SemesterId + ProfessorId).
// 5) Create CourseOffering entity and persist.
// 6) Return new CourseOfferingId.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Commands.CreateCourseOffering;

public class CreateCourseOfferingCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<CreateCourseOfferingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCourseOfferingCommand request, CancellationToken ct)
    {
        // 1) Validate Course
        var courses = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.Course, Guid>().GetAllAsync();
        if (!courses.Any(c => c.Id == request.CourseId))
            return Result<Guid>.Fail(
                Error.NotFound("CourseOffering.CourseNotFound", $"Course {request.CourseId} not found."));

        // 2) Validate Semester
        var semesters = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>().GetAllAsync();
        if (!semesters.Any(s => s.Id == request.SemesterId))
            return Result<Guid>.Fail(
                Error.NotFound("CourseOffering.SemesterNotFound", $"Semester {request.SemesterId} not found."));

        // 3) Resolve Professor by AppUserId (the AppUser FK, not the Professor PK)
        var professors = await _uow.GetRepository<Professor, Guid>().GetAllAsync();
        var professor = professors.FirstOrDefault(p => p.AppUserId == request.ProfessorAppUserId);
        if (professor is null)
            return Result<Guid>.Fail(
                Error.NotFound("CourseOffering.ProfessorNotFound",
                    $"Professor with AppUserId '{request.ProfessorAppUserId}' not found."));

        // 4) Duplicate guard
        var offerings = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().GetAllAsync();
        if (offerings.Any(o =>
            o.CourseId    == request.CourseId    &&
            o.SemesterId  == request.SemesterId  &&
            o.ProfessorId == professor.Id))
            return Result<Guid>.Fail(
                Error.Validation("CourseOffering.Duplicate", "This course offering already exists."));

        // 5) Persist
        var offering = new BNU_Student_Portal_Domain.Entities.Courses.CourseOffering
        {
            Id          = Guid.NewGuid(),
            CourseId    = request.CourseId,
            SemesterId  = request.SemesterId,
            ProfessorId = professor.Id
        };

        await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().AddAsync(offering);
        await _uow.SaveChangesAsync();

        return Result<Guid>.Ok(offering.Id);
    }
}
