// FILE: Features/Admin/CourseOffering/Commands/CreateCourseOffering/CreateCourseOfferingCommand.cs
// PURPOSE: Admin links a Course + Semester + Professor into one CourseOffering.
//
// FLOW DIAGRAM:
// [POST /api/admin/course-offerings]
//    -> [CreateCourseOfferingCommand(CourseId, SemesterId, ProfessorId)]
//    -> [CreateCourseOfferingCommandHandler]
//         | validate FK existence
//         | prevent duplicates
//         | persist CourseOffering
//    -> returns CourseOfferingId (used to create sections)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Commands.CreateCourseOffering;

public record CreateCourseOfferingCommand(
    Guid CourseId,
    Guid SemesterId,
    Guid ProfessorId)
    : IRequest<Result<Guid>>;
