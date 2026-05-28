// FILE: Features/Admin/CourseOffering/Commands/CreateCourseOffering/CreateCourseOfferingCommandHandler.cs
// PURPOSE: Validates all FKs exist then persists a new CourseOffering row.
//
// IMPLEMENTATION FLOW:
// 1) Validate Course, Semester, Professor exist.
// 2) Ensure no duplicate (CourseId + SemesterId + ProfessorId).
// 3) Create CourseOffering entity and persist.
// 4) Return new CourseOfferingId.
//
// DIAGRAM:
// Validate FKs -> Check Duplicate -> Create -> Save -> Return Id

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
        var courses = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        if (!courses.Any(c => c.Id == request.CourseId))
            return Result<Guid>.Fail(
                Error.NotFound("CourseOffering.CourseNotFound", $"Course {request.CourseId} not found."));

        var semesters = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>().GetAllAsync();
        if (!semesters.Any(s => s.Id == request.SemesterId))
            return Result<Guid>.Fail(
                Error.NotFound("CourseOffering.SemesterNotFound", $"Semester {request.SemesterId} not found."));

        var professors = await _uow.GetRepository<Professor, Guid>().GetAllAsync();
        if (!professors.Any(p => p.Id == request.ProfessorId))
            return Result<Guid>.Fail(
                Error.NotFound("CourseOffering.ProfessorNotFound", $"Professor {request.ProfessorId} not found."));

        var offerings = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().GetAllAsync();
        if (offerings.Any(o =>
            o.CourseId    == request.CourseId &&
            o.SemesterId  == request.SemesterId &&
            o.ProfessorId == request.ProfessorId))
            return Result<Guid>.Fail(
                Error.Validation("CourseOffering.Duplicate", "This course offering already exists."));

        var offering = new BNU_Student_Portal_Domain.Entities.Courses.CourseOffering
        {
            Id          = Guid.NewGuid(),
            CourseId    = request.CourseId,
            SemesterId  = request.SemesterId,
            ProfessorId = request.ProfessorId
        };

        await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().AddAsync(offering);
        await _uow.SaveChangesAsync();

        return Result<Guid>.Ok(offering.Id);
    }
}
