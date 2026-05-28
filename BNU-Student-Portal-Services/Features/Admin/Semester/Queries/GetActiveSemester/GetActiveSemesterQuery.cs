// FILE: Features/Admin/Semester/Queries/GetActiveSemester/GetActiveSemesterQuery.cs
// PURPOSE: Admin reads which semester is currently active.
//
// FLOW DIAGRAM:
// [GET /api/admin/semesters/active]
//    -> [GetActiveSemesterQuery]
//    -> [GetActiveSemesterQueryHandler]
//    -> returns SemesterDto (404 if none active)

using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetActiveSemester;

public record GetActiveSemesterQuery : IRequest<Result<SemesterDto>>;
