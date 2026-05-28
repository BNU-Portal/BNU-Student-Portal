// ============================================================
// FILE: Features/Grades/TA/Commands/UpdateAttendance/UpdateAttendanceCommandHandler.cs
// LAYER: Application — CQRS Command Handler
// ============================================================
//
// PURPOSE:
//   Handles the UpdateAttendanceCommand by:
//     1. Proving the caller is a registered TA
//     2. Proving the grade record belongs to a section this TA owns
//     3. Rejecting edits to already-published grades
//     4. Mutating AttendanceScore + AttendanceOverridden and saving
//
// SECURITY MODEL:
//
//   JWT (TA role) → CallerAppUserId
//         │
//         ▼
//   TeachingAssistant table  →  ta.Id
//         │
//         ▼
//   CourseGrade.EnrollmentId → StudentSectionEnrollment.CourseSectionId
//         │
//         ▼
//   CourseSection.TeachingAssistantId  ──must equal──  ta.Id
//         │
//         ▼
//   If mismatch → 403 Forbidden (TA cannot touch another TA's student)
//
// PUBLISH GATE:
//
//   grade.IsPublished == true?
//         YES → 400 Bad Request ("Ask the professor to unpublish")
//         NO  → proceed with mutation + save
//
// DATA CHAIN (in-memory joins, no EF navigation properties):
//
//   CourseGrade
//       └─ EnrollmentId ──→ StudentSectionEnrollment
//                               └─ CourseSectionId ──→ CourseSection
//                                                          └─ TeachingAssistantId
//
// NOTE:
//   CourseGrade is a class — mutate directly. No 'with' needed.
// ============================================================

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.UpdateAttendance;

public class UpdateAttendanceCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<UpdateAttendanceCommand, Result>
{
    public async Task<Result> Handle(UpdateAttendanceCommand request, CancellationToken ct)
    {
        // ── Step 1: Resolve TA from JWT AppUserId ─────────────────────────────────
        // AppUserId is the IdentityUser.Id string stored in the JWT claim.
        // We need the TeachingAssistant domain entity (with ta.Id as Guid)
        // to match against CourseSection.TeachingAssistantId.
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == request.CallerAppUserId);
        if (ta is null)
            return Result<object>.Fail(Error.NotFound("Grades.TaNotFound", "Teaching Assistant not found."));

        // ── Step 2: Load the grade record ─────────────────────────────────────────
        // CourseGrade is the parent record that holds midterm, final, attendance,
        // and the IsPublished flag. Each student-enrollment has exactly one.
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result<object>.Fail(Error.NotFound("Grades.GradeNotFound", "Grade record not found."));

        // ── Step 3: Verify grade belongs to a section assigned to this TA ─────────
        // Walk: grade → enrollment → section → check TeachingAssistantId
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();

        var enrollment = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        var section    = sections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);

        if (section?.TeachingAssistantId != ta.Id)
            return Result<object>.Fail(Error.Forbidden("Grades.Forbidden",
                "This student is not in your section."));

        // ── Step 4: Block edits on published grades ────────────────────────────────
        // Once the professor publishes, the TA cannot edit until the professor
        // calls UnpublishGrade (professor-only action).
        if (grade.IsPublished)
            return Result<object>.Fail(Error.BadRequest("Grades.AlreadyPublished",
                "Grade is published. Ask the professor to unpublish it before editing."));

        // ── Step 5: Apply attendance update and persist ────────────────────────────
        // CourseGrade is a class — mutate the properties directly.
        // AttendanceOverridden = true tells GradeCalculator / QR system not to
        // recalculate this field automatically.
        grade.AttendanceScore      = request.AttendanceScore;
        grade.AttendanceOverridden = request.AttendanceOverridden;

        _uow.GetRepository<CourseGrade, Guid>().Update(grade);
        await _uow.SaveChangesAsync();

        return Result<object>.Ok("Attendance updated successfully.");
    }
}
