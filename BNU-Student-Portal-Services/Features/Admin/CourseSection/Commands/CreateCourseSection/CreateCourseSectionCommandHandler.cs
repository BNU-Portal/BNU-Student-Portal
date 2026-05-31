// FILE: Features/Admin/CourseSection/Commands/CreateCourseSection/CreateCourseSectionCommandHandler.cs
// PURPOSE: Validates FKs then creates a CourseSection.
//          SemesterId is copied from the CourseOffering (immutable after creation).
//
// IMPLEMENTATION FLOW:
// 1) Validate CourseOffering exists.
// 2) Resolve TeachingAssistant by AppUserId (NOT TA.Id PK). Return 404 if not found.
// 3) Copy SemesterId from CourseOffering to section.
// 4) Persist CourseSection and return Id.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseSection.Commands.CreateCourseSection;

public class CreateCourseSectionCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<CreateCourseSectionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCourseSectionCommand request, CancellationToken ct)
    {
        // 1) Validate CourseOffering
        var offerings = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseOffering, Guid>().GetAllAsync();
        var offering  = offerings.FirstOrDefault(o => o.Id == request.CourseOfferingId);
        if (offering is null)
            return Result<Guid>.Fail(
                Error.NotFound("CourseSection.OfferingNotFound",
                    $"CourseOffering {request.CourseOfferingId} not found."));

        // 2) Resolve TA by AppUserId (not PK)
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == request.TaAppUserId);
        if (ta is null)
            return Result<Guid>.Fail(
                Error.NotFound("CourseSection.TaNotFound",
                    $"TeachingAssistant with AppUserId '{request.TaAppUserId}' not found."));

        // 3) Build section — inherit SemesterId from offering
        var section = new BNU_Student_Portal_Domain.Entities.Courses.CourseSection
        {
            Id                  = Guid.NewGuid(),
            CourseOfferingId    = offering.Id,
            SemesterId          = offering.SemesterId,
            SectionName         = request.SectionName,
            MaxStudents         = request.MaxStudents,
            TeachingAssistantId = ta.Id
        };

        await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseSection, Guid>().AddAsync(section);
        await _uow.SaveChangesAsync();

        return Result<Guid>.Ok(section.Id);
    }
}
