// FILE: Features/Admin/CourseOffering/Commands/CreateCourseOffering/CreateCourseOfferingCommand.cs
// PURPOSE: Admin links a Course + Semester + Professor into one CourseOffering.
//
// NOTE: ProfessorAppUserId is the AppUser.Id (string FK), NOT the Professor.Id (PK).
//       The handler resolves Professor.Id from AppUserId internally.
//
// FLOW DIAGRAM:
// [POST /api/admin/course-offerings]
//    -> [CreateCourseOfferingCommand(CourseId, SemesterId, ProfessorAppUserId)]
//    -> [CreateCourseOfferingCommandHandler]
//         | validate Course exists
//         | validate Semester exists
//         | resolve Professor by AppUserId -> get Professor.Id (PK)
//         | prevent duplicates
//         | persist CourseOffering
//    -> returns CourseOfferingId (used to create sections)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Commands.CreateCourseOffering;

public record CreateCourseOfferingCommand(
    Guid   CourseId,
    Guid   SemesterId,
    string ProfessorAppUserId)
    : IRequest<Result<Guid>>;
