// FILE: Features/Admin/Semester/Commands/CreateSemester/CreateSemesterCommand.cs
// PURPOSE: Admin creates a new semester. IsActive defaults to false on creation.
//
// FLOW DIAGRAM:
// [POST /api/admin/semesters]
//    -> [CreateSemesterCommand]
//    -> [CreateSemesterCommandHandler]
//    -> [Semester row persisted with IsActive = false]
//    -> returns SemesterId (used by ActivateSemester and CourseOffering)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Commands.CreateSemester;

public record CreateSemesterCommand(
    string   Name,
    DateOnly StartDate,
    DateOnly EndDate)
    : IRequest<Result<Guid>>;
