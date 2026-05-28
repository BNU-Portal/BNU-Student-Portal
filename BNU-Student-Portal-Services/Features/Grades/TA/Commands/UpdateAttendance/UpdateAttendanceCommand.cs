// FILE: Features/Grades/TA/Commands/UpdateAttendance/UpdateAttendanceCommand.cs
// PURPOSE: TA updates the attendance score for one student in their section.
//          AttendanceOverridden = true signals that the TA set this manually
//          (overriding the automatic QR-based calculation).
//          Max attendance score is 5 — enforced at the controller/validator level.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.UpdateAttendance;

public record UpdateAttendanceCommand(
    string  CallerAppUserId,      // from JWT
    Guid    CourseGradeId,        // the grade record to update
    decimal AttendanceScore,      // 0–5
    bool    AttendanceOverridden) // true = manually overridden by TA
    : IRequest<Result>;
