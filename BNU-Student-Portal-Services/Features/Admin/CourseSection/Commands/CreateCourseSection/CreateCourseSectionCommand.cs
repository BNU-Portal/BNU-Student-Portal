// FILE: Features/Admin/CourseSection/Commands/CreateCourseSection/CreateCourseSectionCommand.cs
// PURPOSE: Admin creates a section under a CourseOffering and assigns a TA.
//
// NOTE: TaAppUserId is the AppUser.Id (string FK), NOT the TeachingAssistant.Id (PK).
//       MaxStudents defaults to 25 if not provided.
//       Admin enrollment bypasses the MaxStudents cap.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseSection.Commands.CreateCourseSection;

public record CreateCourseSectionCommand(
    Guid   CourseOfferingId,
    string TaAppUserId,
    string SectionName,
    int    MaxStudents = 25)
    : IRequest<Result<Guid>>;
