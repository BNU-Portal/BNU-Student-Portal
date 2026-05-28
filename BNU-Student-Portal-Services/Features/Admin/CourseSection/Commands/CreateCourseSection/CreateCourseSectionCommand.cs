// FILE: Features/Admin/CourseSection/Commands/CreateCourseSection/CreateCourseSectionCommand.cs
// PURPOSE: Admin creates a section under a CourseOffering and assigns a TA.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseSection.Commands.CreateCourseSection;

public record CreateCourseSectionCommand(
    Guid   CourseOfferingId,
    Guid   TeachingAssistantId,
    string SectionName)
    : IRequest<Result<Guid>>;
