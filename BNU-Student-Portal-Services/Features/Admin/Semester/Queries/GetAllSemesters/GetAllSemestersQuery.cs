// FILE: Features/Admin/Semester/Queries/GetAllSemesters/GetAllSemestersQuery.cs
// PURPOSE: Admin reads the full semester list for dropdowns and management views.
//
// FLOW DIAGRAM:
// [GET /api/admin/semesters]
//    -> [GetAllSemestersQuery]
//    -> [GetAllSemestersQueryHandler]
//    -> returns ordered SemesterDto list

using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetAllSemesters;

public record GetAllSemestersQuery : IRequest<Result<List<SemesterDto>>>;
