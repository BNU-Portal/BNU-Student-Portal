// FILE: Features/Admin/Semester/Queries/GetActiveSemester/GetActiveSemesterQueryHandler.cs
// PURPOSE: Find the single Semester with IsActive = true and return it as a DTO.
//
// IMPLEMENTATION FLOW:
// 1) Load all semesters.
// 2) Find the first active one.
// 3) Return 404 if none.
// 4) Map to SemesterDto and return.
//
// DIAGRAM:
// Load -> Find Active -> Map -> Return

using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetActiveSemester;

public class GetActiveSemesterQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetActiveSemesterQuery, Result<SemesterDto>>
{
    public async Task<Result<SemesterDto>> Handle(
        GetActiveSemesterQuery request, CancellationToken ct)
    {
        var semesters = await _uow
            .GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>()
            .GetAllAsync();

        var active = semesters.FirstOrDefault(s => s.IsActive);

        if (active is null)
            return Result<SemesterDto>.Fail(
                Error.NotFound("Semester.NoActive", "No active semester found."));

        return Result<SemesterDto>.Ok(new SemesterDto
        {
            Id        = active.Id,
            Name      = active.Name,
            StartDate = active.StartDate,
            EndDate   = active.EndDate,
            IsActive  = active.IsActive
        });
    }
}
