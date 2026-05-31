// FILE: Features/Admin/Course/Commands/CreateCourse/CreateCourseCommand.cs
// PURPOSE: Admin creates a new course catalogue entry.
//
// FLOW DIAGRAM:
// [POST /api/admin/courses]
//    -> [CreateCourseCommand(Code, Name, CreditHours)]
//    -> [CreateCourseCommandHandler]
//         | check duplicate Code
//         | create Course entity
//         | Add + SaveChanges
//    -> returns CourseId (used by CreateCourseOffering)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Course.Commands.CreateCourse;

public record CreateCourseCommand(
    string Code,
    string Name,
    int    CreditHours)
    : IRequest<Result<Guid>>;
