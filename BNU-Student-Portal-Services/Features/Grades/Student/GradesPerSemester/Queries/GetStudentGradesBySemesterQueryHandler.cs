using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Courses;
using BNU_Student_Portal_Shared_Library.DTO_s.Grades;
using BNU_Student_Portal_Shared_Library.DTO_s.Semster;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.GradesPerSemester.Queries;

public class GetStudentGradesBySemesterQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetStudentGradesBySemesterQuery, Result<StudentSemesterSummaryDto>>
{
    public async Task<Result<StudentSemesterSummaryDto>> Handle(
        GetStudentGradesBySemesterQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve student from the AppUserId stored in JWT ──────────
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student = students.FirstOrDefault(s => s.AppUserId == request.CallerAppUserId);
        if (student is null)
            return Result<StudentSemesterSummaryDto>.Fail(
                Error.NotFound("Grades.StudentNotFound", "Student profile not found."));

        // ── Step 2: Validate the requested semester exists ────────────────────
        var semesters = await _uow.GetRepository<Semester, Guid>().GetAllAsync();
        var semester = semesters.FirstOrDefault(s => s.Id == request.SemesterId);
        if (semester is null)
            return Result<StudentSemesterSummaryDto>.Fail(
                Error.NotFound("Grades.SemesterNotFound", "Semester not found."));

        // ── Step 3: Load all required tables into memory (no Include calls) ───
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings   = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var courses     = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var allGrades   = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var quizGrades  = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        var discGrades  = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();

        // ── Step 4: Build a HashSet of this student's enrollment IDs ──────────
        // HashSet gives O(1) lookup instead of O(n) Contains on a list
        var myEnrollmentIds = enrollments
            .Where(e => e.StudentId == student.Id)
            .Select(e => e.Id)
            .ToHashSet();

        // ── Step 5: Join grade → enrollment → section → offering ─────────────
        // Filter to only this semester's offerings at the end
        var semesterGradeRows = allGrades
            .Where(g => myEnrollmentIds.Contains(g.EnrollmentId))
            .Join(enrollments, g => g.EnrollmentId,       e => e.Id, (g, e) => (g, e))
            .Join(sections,    x => x.e.CourseSectionId,  s => s.Id, (x, s) => (x.g, s))
            .Join(offerings,   x => x.s.CourseOfferingId, o => o.Id, (x, o) => (x.g, o))
            .Where(x => x.o.SemesterId == request.SemesterId)
            .ToList();

        // ── Step 6: Build one StudentGradeRowDto per course ───────────────────
        var rows = semesterGradeRows.Select(x =>
        {
            var (grade, offering) = x;

            // Look up the course for code, name, and credit hours
            var course = courses.FirstOrDefault(c => c.Id == offering.CourseId);

            // Sum all quiz scores that belong to this grade record
            var quizTotal = quizGrades
                .Where(q => q.CourseGradeId == grade.Id)
                .Sum(q => q.Score);

            // Sum all discussion scores that belong to this grade record
            var discTotal = discGrades
                .Where(d => d.CourseGradeId == grade.Id)
                .Sum(d => d.Score);

            // Run the BNU formula → returns (CourseWorkTotal, GrandTotal)
            var (cw, total) = GradeCalculator.Calculate(
                grade.Midterm1Score, grade.Midterm2Score,
                discTotal, grade.AttendanceScore, quizTotal,
                grade.FinalExamScore);

            // Student only sees computed totals when grade is published AND final exam score exists
            var canShow = grade.IsPublished && grade.FinalExamScore.HasValue;
            var letter  = canShow ? GradeCalculator.GetLetterGrade(total)   : null;
            var gpa     = canShow ? GradeCalculator.GetGpaPoints(letter!)   : (decimal?)null;

            // StudentGradeRowDto uses { } property initializer (NOT positional constructor)
            return new StudentGradeRowDto
            {
                CourseGradeId      = grade.Id,
                CourseCode         = course?.Code        ?? string.Empty,
                CourseName         = course?.Name        ?? string.Empty,
                CreditHours        = course?.CreditHours ?? 0,
                Total              = canShow ? total : null,
                LetterGrade        = letter,
                GpaPoints          = gpa,
                IsPublished        = grade.IsPublished,
                HasAcademicWarning = grade.HasAcademicWarning,

                // CourseBreakdownDto IS a positional record — args must match declaration order exactly:
                // (Midterm1Score, Midterm2Score, DiscussionScore, AttendanceScore,
                //  AttendanceOverridden, QuizScore, CourseWorkTotal, FinalExamScore, Total)
                Breakdown = new CourseBreakdownDto(
                    grade.Midterm1Score,          // decimal? Midterm1Score
                    grade.Midterm2Score,          // decimal? Midterm2Score
                    discTotal,                    // decimal? DiscussionScore
                    grade.AttendanceScore,        // decimal? AttendanceScore
                    grade.AttendanceOverridden,   // bool     AttendanceOverridden
                    quizTotal,                    // decimal? QuizScore
                    canShow ? cw    : null,       // decimal? CourseWorkTotal (hidden until published)
                    grade.FinalExamScore,         // decimal? FinalExamScore
                    canShow ? total : null        // decimal? Total           (hidden until published)
                )
            };
        }).ToList();

        // ── Step 7: Cumulative GPA across ALL semesters (not just this one) ───
        // We need to join again to reach credit hours per course
        var allPublishedForCumulative = allGrades
            .Where(g => myEnrollmentIds.Contains(g.EnrollmentId)
                        && g.IsPublished
                        && g.FinalExamScore.HasValue)
            .Join(enrollments, g => g.EnrollmentId,       e => e.Id, (g, e) => (g, e))
            .Join(sections,    x => x.e.CourseSectionId,  s => s.Id, (x, s) => (x.g, s))
            .Join(offerings,   x => x.s.CourseOfferingId, o => o.Id, (x, o) => (x.g, o))
            .Join(courses,     x => x.o.CourseId,         c => c.Id, (x, c) =>
            (
                // Recalculate total for each published course across all semesters
                Total: GradeCalculator.Calculate(
                    x.g.Midterm1Score, x.g.Midterm2Score,
                    discGrades.Where(d => d.CourseGradeId == x.g.Id).Sum(d => d.Score),
                    x.g.AttendanceScore,
                    quizGrades.Where(q => q.CourseGradeId == x.g.Id).Sum(q => q.Score),
                    x.g.FinalExamScore).Total,
                c.CreditHours
            ))
            .ToList();

        // Weighted GPA across every semester this student has completed
        var cumulativeGpa = GradeCalculator.CalculateGpa(allPublishedForCumulative);

        // Total credit hours earned across ALL semesters
        var totalCredits = allPublishedForCumulative.Sum(x => x.CreditHours);

        // ── Step 8: Semester GPA for THIS semester only (reuse rows list) ─────
        var semesterGpa = GradeCalculator.CalculateGpa(
            rows.Where(r => r.IsPublished && r.Total.HasValue)
                .Select(r => (r.Total!.Value, r.CreditHours)));

        // ── Step 9: Highest letter grade this semester (for the UI badge) ─────
        var highestGrade = rows
            .Where(r => r.GpaPoints.HasValue)
            .OrderByDescending(r => r.GpaPoints)
            .FirstOrDefault()?.LetterGrade;

        // ── Step 10: Build the response ───────────────────────────────────────
        // StudentSemesterSummaryDto IS a positional record — argument order is law:
        // (Guid SemesterId, string SemesterName, decimal? SemesterGpa,
        //  int TotalCreditHours, decimal? CumulativeGpa,
        //  string? HighestGrade, int CurrentSemesterCourseCount,
        //  IEnumerable<StudentGradeRowDto> Grades)
        return Result<StudentSemesterSummaryDto>.Ok(new StudentSemesterSummaryDto(
            semester.Id,      // Guid     SemesterId
            semester.Name,    // string   SemesterName
            semesterGpa,      // decimal? SemesterGpa               ← was rows.Count (WRONG TYPE)
            totalCredits,     // int      TotalCreditHours
            cumulativeGpa,    // decimal? CumulativeGpa
            highestGrade,     // string?  HighestGrade
            rows.Count,       // int      CurrentSemesterCourseCount ← was undefined variable before
            rows              // IEnumerable<StudentGradeRowDto> Grades
        ));
    }
}
