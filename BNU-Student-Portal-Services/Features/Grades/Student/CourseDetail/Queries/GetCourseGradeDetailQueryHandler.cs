// FILE: Features/Grades/Student/CourseDetail/Queries/GetCourseGradeDetailQueryHandler.cs
// PURPOSE: Handles Q3 — student drill-down into one course.
//
// FLOW:
//   1. Resolve student from JWT
//   2. Load the CourseGrade and verify it belongs to this student
//   3. Load all QuizGrades for this CourseGrade → join Quiz for title/maxScore
//   4. Load all DiscussionGrades for this CourseGrade → join Discussion for title/maxScore
//   5. Run GradeCalculator for CW + Total
//   6. Apply visibility gate (same rule as semester query)
//   7. Return CourseGradeDetailDto

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Discussions;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Quizzes;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.CourseDetail.Queries;

public class GetCourseGradeDetailQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetCourseGradeDetailQuery, Result<CourseGradeDetailDto>>
{
    public async Task<Result<CourseGradeDetailDto>> Handle(
        GetCourseGradeDetailQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve student ──────────────────────────────────────────
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student  = students.FirstOrDefault(s => s.AppUserId == request.CallerAppUserId);
        if (student is null)
            return Result<CourseGradeDetailDto>.Fail(
                Error.NotFound("Grades.StudentNotFound", "Student profile not found."));

        // ── Step 2: Load CourseGrade and verify ownership ────────────────────
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result<CourseGradeDetailDto>.Fail(
                Error.NotFound("Grades.NotFound", "Course grade not found."));

        // Verify this grade belongs to the calling student
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var enrollment  = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        if (enrollment is null || enrollment.StudentId != student.Id)
            return Result<CourseGradeDetailDto>.Fail(
                Error.Forbidden("Grades.Forbidden", "You do not have access to this grade."));

        // ── Step 3: Load course info via section → offering → course ─────────
        var sections  = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var courses   = await _uow.GetRepository<Course, Guid>().GetAllAsync();

        var section  = sections.FirstOrDefault(s => s.Id == enrollment.CourseSectionId);
        var offering = section is null ? null : offerings.FirstOrDefault(o => o.Id == section.CourseOfferingId);
        var course   = offering is null ? null : courses.FirstOrDefault(c => c.Id == offering.CourseId);

        // ── Step 4: Load individual quiz scores ──────────────────────────────
        var allQuizGrades = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        var allQuizzes    = await _uow.GetRepository<Quiz, Guid>().GetAllAsync();

        var quizItems = allQuizGrades
            .Where(qg => qg.CourseGradeId == grade.Id)
            .Join(allQuizzes, qg => qg.QuizId, q => q.Id,
                  (qg, q) => new QuizDetailItem(
                      Title:    q.Title,
                      Score:    qg.Score,
                      MaxScore: q.MaxScore,
                      Note:     qg.Note))
            .ToList();

        var quizTotal = quizItems.Sum(q => q.Score);

        // ── Step 5: Load individual discussion scores ────────────────────────
        var allDiscGrades   = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();
        var allDiscussions  = await _uow.GetRepository<Discussion, Guid>().GetAllAsync();

        var discussionItems = allDiscGrades
            .Where(dg => dg.CourseGradeId == grade.Id)
            .Join(allDiscussions, dg => dg.DiscussionId, d => d.Id,
                  (dg, d) => new DiscussionDetailItem(
                      Title:    d.Title,
                      Score:    dg.Score,
                      MaxScore: d.MaxScore,
                      Note:     dg.Note))
            .ToList();

        var discTotal = discussionItems.Sum(d => d.Score);

        // ── Step 6: Calculate totals ─────────────────────────────────────────
        var (cw, total) = GradeCalculator.Calculate(
            grade.Midterm1Score, grade.Midterm2Score,
            discTotal, grade.AttendanceScore, quizTotal,
            grade.FinalExamScore);

        // Visibility gate — same rule as semester query
        var canShow    = grade.IsPublished && grade.FinalExamScore.HasValue;
        var letter     = canShow ? GradeCalculator.GetLetterGrade(total) : null;
        var gpaPoints  = canShow ? GradeCalculator.GetGpaPoints(letter!) : (decimal?)null;

        // ── Step 7: Build and return DTO ─────────────────────────────────────
        var dto = new CourseGradeDetailDto(
            CourseGradeId:        grade.Id,
            CourseCode:           course?.Code        ?? string.Empty,
            CourseName:           course?.Name        ?? string.Empty,
            CreditHours:          course?.CreditHours ?? 0,

            Midterm1Score:        grade.Midterm1Score,
            Midterm2Score:        grade.Midterm2Score,
            FinalExamScore:       grade.FinalExamScore,

            AttendanceScore:      grade.AttendanceScore,
            AttendanceOverridden: grade.AttendanceOverridden,

            Quizzes:              quizItems,
            Discussions:          discussionItems,

            ProfNote:             grade.ProfNote,

            CourseWorkTotal:      canShow ? cw    : null,
            Total:                canShow ? total : null,
            LetterGrade:          letter,
            GpaPoints:            gpaPoints,

            IsPublished:          grade.IsPublished,
            HasAcademicWarning:   grade.HasAcademicWarning
        );

        return Result<CourseGradeDetailDto>.Ok(dto);
    }
}
