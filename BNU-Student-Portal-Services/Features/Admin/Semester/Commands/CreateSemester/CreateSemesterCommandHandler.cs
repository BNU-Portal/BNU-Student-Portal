// FILE: Features/Admin/Semester/Commands/CreateSemester/CreateSemesterCommandHandler.cs
// PURPOSE: Persists a new Semester row. IsActive = false by default.
//
// IMPLEMENTATION FLOW:
// 1) Validate dates (EndDate > StartDate).
//    - Fails with Error.Validation("Semester.InvalidDates") if invalid.
// 2) Create Semester entity with IsActive = false.
// 3) Add via UoW repository and SaveChanges.
// 4) Return SemesterId to the caller.
//
// DETAILED FLOW DIAGRAM:
//
//   [Admin Request: Create Semester]
//            |
//            v
//   +----------------------+
//   | Date Logic Check     | --- [EndDate > StartDate?] --- (NO) --> [400 Validation]
//   +----------------------+          |
//            |                      (YES)
//            v                        |
//   +----------------------+          v
//   | Entity Initialization| --- Set: Name, StartDate, EndDate
//   | (Default State)      | --- Set: IsActive = false (Admin must manually activate)
//   +----------------------+
//            |
//            v
//   +----------------------+
//   | SQL Database Insert  | <-- INSERT INTO Semesters (...)
//   +----------------------+
//            |
//            v
//   [200 OK: Semester Created]

using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Semester.Commands.CreateSemester;

public class CreateSemesterCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<CreateSemesterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSemesterCommand request, CancellationToken ct)
    {
        if (request.EndDate <= request.StartDate)
            return Result<Guid>.Fail(
                Error.Validation("Semester.InvalidDates", "EndDate must be after StartDate."));

        var semester = new BNU_Student_Portal_Domain.Entities.Semesters.Semester
        {
            Id        = Guid.NewGuid(),
            Name      = request.Name,
            StartDate = request.StartDate,
            EndDate   = request.EndDate,
            IsActive  = false
        };

        await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Semesters.Semester, Guid>().AddAsync(semester);
        await _uow.SaveChangesAsync();

        return Result<Guid>.Ok(semester.Id);
    }
}
