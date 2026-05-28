// FILE: Features/Admin/CourseSection/Commands/CreateCourseSection/CreateCourseSectionCommand.cs
// PURPOSE: Admin creates a section under a CourseOffering and assigns a TA.
//
// FLOW DIAGRAM:
// [POST /api/admin/course-sections]
//    -> [CreateCourseSectionCommand(CourseOfferingId, TeachingAssistantId, SectionName)]
//    -> [CreateCourseSectionCommandHandler]
//         | validate offering + TA
//         | copy SemesterId from offering
//         | persist section
//    -> returns CourseSectionId (used for enrollments)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseSection.Commands.CreateCourseSection;

public record CreateCourseSectionCommand(
    Guid   CourseOfferingId,
    Guid   TeachingAssistantId,
    string SectionName)
    : IRequest<Result<Guid>>;
