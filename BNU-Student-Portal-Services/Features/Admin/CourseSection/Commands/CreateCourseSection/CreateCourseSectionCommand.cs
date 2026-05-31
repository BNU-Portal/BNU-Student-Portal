// FILE: Features/Admin/CourseSection/Commands/CreateCourseSection/CreateCourseSectionCommand.cs
// PURPOSE: Admin creates a section under a CourseOffering and assigns a TA.
//
// NOTE: TaAppUserId is the AppUser.Id (string FK), NOT the TeachingAssistant.Id (PK).
//       The handler resolves TeachingAssistant.Id from AppUserId internally.
//
// FLOW DIAGRAM:
// [POST /api/admin/course-sections]
//    -> [CreateCourseSectionCommand(CourseOfferingId, TaAppUserId, SectionName)]
//    -> [CreateCourseSectionCommandHandler]
//         | validate offering exists
//         | resolve TA by AppUserId -> get TeachingAssistant.Id (PK)
//         | copy SemesterId from offering
//         | persist section
//    -> returns CourseSectionId (used for enrollments)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseSection.Commands.CreateCourseSection;

public record CreateCourseSectionCommand(
    Guid   CourseOfferingId,
    string TaAppUserId,
    string SectionName)
    : IRequest<Result<Guid>>;
