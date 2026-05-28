// FILE: Features/Admin/Semester/Commands/ActivateSemester/ActivateSemesterCommand.cs
// PURPOSE: Admin marks one semester as active and deactivates all others.
//
// FLOW DIAGRAM:
// [PUT /api/admin/semesters/{id}/activate]
//    -> [ActivateSemesterCommand]
//    -> [ActivateSemesterCommandHandler]
//    -> [All semesters loaded; only target is active]
//    -> returns success message

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Commands.ActivateSemester;

public record ActivateSemesterCommand(Guid SemesterId) : IRequest<Result>;
