// FILE: Features/Admin/Semester/Queries/GetAllSemesters/GetAllSemestersQueryHandler.cs
// PURPOSE: Fetch all semesters, sort by StartDate desc, map to SemesterDto.
//
// IMPLEMENTATION FLOW:
// 1) Load all Semester rows.
// 2) Order by StartDate descending.
// 3) Map each row to SemesterDto.
// 4) Return list.
//
// DETAILED FLOW DIAGRAM:
//
//   [Request: Get All Semesters]
//            |
//            v
//   +----------------------+
//   | Fetch Repositories   | <-- SELECT * FROM Semesters
//   +----------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Sort Logic           | <--- | OrderByDescending(s => s.StartDate)      |
//   | (Ordering)           |      | (Newest semesters appear at the top)    |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+
//   | Map to DTOs          | --- List<SemesterDto>
//   +----------------------+
//            |
//            v
//   [200 OK: Full Semester List]

using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetAllSemesters;

public class GetAllSemestersQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetAllSemestersQuery, Result<List<SemesterDto>>>
{
    public async Task<Result<List<SemesterDto>>> Handle(
        GetAllSemestersQuery request, CancellationToken ct)
    {
        var semesters = await _uow
            .GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>()
            .GetAllAsync();

        var dtos = semesters
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SemesterDto
            {
                Id        = s.Id,
                Name      = s.Name,
                StartDate = s.StartDate,
                EndDate   = s.EndDate,
                IsActive  = s.IsActive
            })
            .ToList();

        return Result<List<SemesterDto>>.Ok(dtos);
    }
}
