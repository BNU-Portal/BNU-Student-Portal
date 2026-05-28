// ============================================================
// FILE: Features/Grades/TA/Commands/UpdateAttendance/UpdateAttendanceCommand.cs
// LAYER: Application — CQRS Command (write-side)
// ============================================================
//
// PURPOSE:
//   A Teaching Assistant (TA) manually overrides the attendance score for one
//   student in their assigned section.
//
//   Attendance is normally calculated automatically via QR-code scanning.
//   When the TA calls this command, AttendanceOverridden is set to true,
//   which tells the system (and the professor) that a human has manually
//   adjusted the value and it should NOT be recalculated by the QR flow.
//
// MAX SCORE: 5 pts — enforced at the controller validator level (FluentValidation).
//
// WHO CAN CALL THIS:
//   Only a user with the "TeachingAssistant" role. The controller extracts CallerAppUserId
//   from the JWT NameIdentifier claim and passes it in.
//
// FLOW DIAGRAM:
//
//   HTTP PUT /api/grades/ta/attendance
//         │
//         │  Body: { CourseGradeId, AttendanceScore, AttendanceOverridden }
//         ▼
//   [GradesController.UpdateAttendance]
//         │  extracts CallerAppUserId from JWT
//         ▼
//   MediatR.Send(UpdateAttendanceCommand)
//         │
//         ▼
//   UpdateAttendanceCommandHandler.Handle()
//         │  1. Resolve TA from AppUserId
//         │  2. Load CourseGrade by CourseGradeId
//         │  3. Verify grade.Enrollment.Section.TeachingAssistantId == ta.Id
//         │  4. Block if grade.IsPublished == true
//         │  5. grade.AttendanceScore = AttendanceScore
//         │     grade.AttendanceOverridden = AttendanceOverridden
//         │  6. _uow.SaveChangesAsync()
//         ▼
//   Result.Ok("Attendance updated successfully.")
//
// IMPORTANT NOTES:
//   - CourseGrade is a class (not a record), so properties are mutated in-place.
//     No 'with' expression is needed or valid here.
//   - A published grade is immutable from the TA's side. The professor must
//     call UnpublishGrade before the TA can edit again.
//   - AttendanceOverridden = true prevents QR recalculation from overwriting
//     the value the TA set here.
// ============================================================

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.UpdateAttendance;

public record UpdateAttendanceCommand(
    string  CallerAppUserId,      // from JWT
    Guid    CourseGradeId,        // the grade record to update
    decimal AttendanceScore,      // 0–5
    bool    AttendanceOverridden) // true = manually overridden by TA
    : IRequest<Result>;
