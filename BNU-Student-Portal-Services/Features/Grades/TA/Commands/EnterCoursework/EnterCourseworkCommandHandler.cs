// ============================================================
// FILE: Features/Grades/TA/Commands/EnterCoursework/EnterCourseworkCommandHandler.cs
// LAYER: Application — CQRS Command Handler
// ============================================================
//
// PURPOSE:
//   Handles EnterCourseworkCommand:
//     - Validates TA identity and section ownership
//     - Blocks writes to published grades
//     - Updates each QuizGrade and DiscussionGrade child row by its ID
//     - Saves all changes in a single transaction
//
// FULL EXECUTION FLOW:
//
//   EnterCourseworkCommand arrives
//         │
//         ▼
//   [1] Resolve TA (AppUserId → TeachingAssistant row)
//         │  Fail → 404 "TA not found"
//         ▼
//   [2] Load CourseGrade by CourseGradeId
//         │  Fail → 404 "Grade record not found"
//         ▼
//   [3] Section ownership check:
//         CourseGrade.EnrollmentId
//             → StudentSectionEnrollment.CourseSectionId
//                 → CourseSection.TeachingAssistantId  == ta.Id?
//         Mismatch → 403 Forbidden
//         ▼
//   [4] Publish gate:
//         grade.IsPublished == true?
//             YES → 400 Bad Request
//             NO  → continue
//         ▼
//   [5] For each QuizScoreItem in request.QuizScores:
//         Find QuizGrade where Id == item.QuizGradeId
//                            AND CourseGradeId == grade.Id
//         If found → quizGrade.Score = item.Score
//                    _uow.GetRepository<QuizGrade>().Update(quizGrade)
//         If not found → skip (unknown ID, likely stale)
//         ▼
//   [6] For each DiscussionScoreItem in request.DiscussionScores:
//         Same pattern as [5] but for DiscussionGrade
//         ▼
//   [7] _uow.SaveChangesAsync()
//         All updates committed in ONE database transaction
//         ▼
//   Result.Ok("Coursework scores updated successfully.")
//
// CHILD RECORD OWNERSHIP GUARD:
//   The double condition on line [5]:
//       q.Id == item.QuizGradeId && q.CourseGradeId == grade.Id
//   ensures a TA cannot accidentally (or maliciously) update a QuizGrade
//   from a different student's grade record, even if they somehow know the ID.
//
// NOTE:
//   QuizGrade and DiscussionGrade are classes — mutate Score directly.
//   No 'with' expression is needed.
// ============================================================

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.EnterCoursework;

public class EnterCourseworkCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<EnterCourseworkCommand, Result>
{
    public async Task<Result> Handle(EnterCourseworkCommand request, CancellationToken ct)
    {
        // ── Step 1: Resolve TA ────────────────────────────────────────────────────
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == request.CallerAppUserId);
        if (ta is null)
            return Result<object>.Fail(Error.NotFound("Grades.TaNotFound", "Teaching Assistant not found."));

        // ── Step 2: Load the parent grade record ──────────────────────────────────
        // CourseGrade is the central record that all quiz/discussion scores attach to.
        // One CourseGrade exists per StudentSectionEnrollment.
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result<object>.Fail(Error.NotFound("Grades.GradeNotFound", "Grade record not found."));

        // ── Step 3: Verify grade belongs to this TA's section ─────────────────────
        // We walk the chain: grade → enrollment → section → check TA assignment.
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();

        var enrollment = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        var section    = sections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);

        if (section?.TeachingAssistantId != ta.Id)
            return Result<object>.Fail(Error.Forbidden("Grades.Forbidden",
                "This student is not in your section."));

        // ── Step 4: Block edits on published grades ────────────────────────────────
        // Professor must unpublish before TA can edit coursework again.
        if (grade.IsPublished)
            return Result<object>.Fail(Error.BadRequest("Grades.AlreadyPublished",
                "Grade is published. Ask the professor to unpublish it before editing."));

        // ── Step 5: Update each QuizGrade child record ────────────────────────────
        // Double-condition: ID match AND must belong to THIS grade (prevents cross-student updates).
        // Unknown IDs are silently skipped — frontend may send stale data.
        var allQuiz = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        foreach (var item in request.QuizScores)
        {
            var quizGrade = allQuiz.FirstOrDefault(q =>
                q.Id == item.QuizGradeId && q.CourseGradeId == grade.Id);
            if (quizGrade is null) continue;

            quizGrade.Score = item.Score;
            _uow.GetRepository<QuizGrade, Guid>().Update(quizGrade);
        }

        // ── Step 6: Update each DiscussionGrade child record ──────────────────────
        // Same pattern as quizzes — update existing rows, skip unknowns.
        var allDisc = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();
        foreach (var item in request.DiscussionScores)
        {
            var discGrade = allDisc.FirstOrDefault(d =>
                d.Id == item.DiscussionGradeId && d.CourseGradeId == grade.Id);
            if (discGrade is null) continue;

            discGrade.Score = item.Score;
             _uow.GetRepository<DiscussionGrade, Guid>().Update(discGrade);
        }

        // ── Step 7: Persist all changes in one transaction ────────────────────────
        // All quiz and discussion updates above are queued in the UoW change tracker.
        // This single SaveChangesAsync commits everything atomically.
        await _uow.SaveChangesAsync();
        return Result<object>.Ok("Coursework scores updated successfully.");
    }
}
