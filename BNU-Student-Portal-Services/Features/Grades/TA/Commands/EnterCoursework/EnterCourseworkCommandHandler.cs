// FILE: Features/Grades/TA/Commands/EnterCoursework/EnterCourseworkCommandHandler.cs
// PURPOSE: Validate TA owns the section, check publish state, then update each
//          QuizGrade and DiscussionGrade child record by its ID.
//          Unknown IDs are skipped silently — the TA can only update existing rows.
// NOTE:    QuizGrade and DiscussionGrade are classes — mutate directly, no 'with'.

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
            return Result.Fail(Error.NotFound("Grades.TaNotFound", "Teaching Assistant not found."));

        // ── Step 2: Load the parent grade record ──────────────────────────────────
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result.Fail(Error.NotFound("Grades.GradeNotFound", "Grade record not found."));

        // ── Step 3: Verify grade belongs to this TA's section ─────────────────────
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();

        var enrollment = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        var section    = sections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);

        if (section?.TeachingAssistantId != ta.Id)
            return Result.Fail(Error.Forbidden("Grades.Forbidden",
                "This student is not in your section."));

        // ── Step 4: Block edits on published grades ────────────────────────────────
        if (grade.IsPublished)
            return Result.Fail(Error.BadRequest("Grades.AlreadyPublished",
                "Grade is published. Ask the professor to unpublish it before editing."));

        // ── Step 5: Update each QuizGrade child record ────────────────────────────
        // Only update rows that belong to this CourseGrade — ignore unknown IDs.
        var allQuiz = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        foreach (var item in request.QuizScores)
        {
            var quizGrade = allQuiz.FirstOrDefault(q =>
                q.Id == item.QuizGradeId && q.CourseGradeId == grade.Id);
            if (quizGrade is null) continue;

            quizGrade.Score = item.Score;
            await _uow.GetRepository<QuizGrade, Guid>().UpdateAsync(quizGrade);
        }

        // ── Step 6: Update each DiscussionGrade child record ──────────────────────
        var allDisc = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();
        foreach (var item in request.DiscussionScores)
        {
            var discGrade = allDisc.FirstOrDefault(d =>
                d.Id == item.DiscussionGradeId && d.CourseGradeId == grade.Id);
            if (discGrade is null) continue;

            discGrade.Score = item.Score;
            await _uow.GetRepository<DiscussionGrade, Guid>().UpdateAsync(discGrade);
        }

        // ── Step 7: Persist all changes in one transaction ────────────────────────
        await _uow.SaveChangesAsync();
        return Result.Ok();
    }
}
