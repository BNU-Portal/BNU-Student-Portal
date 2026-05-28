// FILE: Features/Admin/Semester/Commands/CreateSemester/CreateSemesterCommand.cs
// PURPOSE: Admin creates a new semester. IsActive defaults to false on creation.
//
// FLOW DIAGRAM (request -> persistence):
// [POST /api/admin/semesters]
//    -> [CreateSemesterCommand(Name, StartDate, EndDate)]
//    -> [CreateSemesterCommandHandler]
//         | validate StartDate < EndDate
//         | create Semester { IsActive = false }
//         | Add + SaveChanges
//    -> returns SemesterId (used by ActivateSemester and CourseOffering)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Commands.CreateSemester;

public record CreateSemesterCommand(
    string   Name,
    DateOnly StartDate,
    DateOnly EndDate)
    : IRequest<Result<Guid>>;
