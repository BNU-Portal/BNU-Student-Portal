// FILE: Features/Admin/Semester/Queries/GetAllSemesters/GetAllSemestersQuery.cs

using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetAllSemesters;

public record GetAllSemestersQuery : IRequest<Result<List<SemesterDto>>>;
