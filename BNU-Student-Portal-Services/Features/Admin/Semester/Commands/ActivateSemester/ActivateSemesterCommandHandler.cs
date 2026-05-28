// FILE: Features/Admin/Semester/Commands/ActivateSemester/ActivateSemesterCommandHandler.cs
// PURPOSE: Sets target semester IsActive = true, all others IsActive = false.
//
// IMPLEMENTATION FLOW:
// 1) Load all semesters.
// 2) Find target semester by id (404 if not found).
// 3) Loop all semesters and flip IsActive based on target.
// 4) Save changes once.
//
// DIAGRAM:
// Load -> Find Target -> Toggle All -> Save

using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Commands.ActivateSemester;

public class ActivateSemesterCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<ActivateSemesterCommand, Result>
{
    public async Task<Result> Handle(ActivateSemesterCommand request, CancellationToken ct)
    {
        var repo      = _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>();
        var semesters = await repo.GetAllAsync();
        var target    = semesters.FirstOrDefault(s => s.Id == request.SemesterId);

        if (target is null)
            return Result<object>.Fail(
                Error.NotFound("Semester.NotFound", $"Semester {request.SemesterId} not found."));

        foreach (var s in semesters)
        {
            s.IsActive = s.Id == request.SemesterId;
            repo.Update(s);
        }

        await _uow.SaveChangesAsync();
        return Result<object>.Ok("Semester activated successfully.");
    }
}
