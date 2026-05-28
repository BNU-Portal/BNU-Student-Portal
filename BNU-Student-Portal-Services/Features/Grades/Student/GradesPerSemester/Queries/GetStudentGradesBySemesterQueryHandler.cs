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
    // FLOW SUMMARY:
    // Resolve student -> validate semester -> load data -> build rows
    // -> compute GPA stats -> return summary DTO
    //
    // DIAGRAM:
    // Student(AppUserId)
    //   -> Enrollments -> Sections -> Offerings (filter SemesterId)
    //   -> CourseGrades -> (Quiz/Discussion) -> GradeCalculator
    //   -> StudentSemesterSummaryDto
    public async Task<Result<StudentSemesterSummaryDto>> Handle(
        GetStudentGradesBySemesterQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve student ──────────────────────────────────────────
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student = students.FirstOrDefault(s => s.AppUserId == request.CallerAppUserId);
        if (student is null)
            return Result<StudentSemesterSummaryDto>.Fail(
                Error.NotFound("Grades.StudentNotFound", "Student profile not found."));

        // Validate the semester exists
        var semesters = await _uow.GetRepository<Semester, Guid>().GetAllAsync();
        var semester = semesters.FirstOrDefault(s => s.Id == request.SemesterId);
        if (semester is null)
            return Result<StudentSemesterSummaryDto>.Fail(
                Error.NotFound("Grades.SemesterNotFound", "Semester not found."));

        // ── Step 2: Load all tables into memory ──────────────────────────────
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var courses = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var quizGrades = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        var discGrades = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();

        // ── Step 3: Collect this student's enrollment IDs (O(1) lookups) ─────
        var myEnrollmentIds = enrollments
            .Where(e => e.StudentId == student.Id)
            .Select(e => e.Id)
            .ToHashSet();

        // ── Step 4: Get grades that belong to the requested semester ──────────
        // Chain: grade → enrollment → section → offering (has SemesterId)
        var semesterGradeRows = allGrades
            .Where(g => myEnrollmentIds.Contains(g.EnrollmentId))
            .Join(enrollments, g => g.EnrollmentId, e => e.Id, (g, e) => (g, e))
            .Join(sections, x => x.e.CourseSectionId, s => s.Id, (x, s) => (x.g, s))
            .Join(offerings, x => x.s.CourseOfferingId, o => o.Id, (x, o) => (x.g, o))
            .Where(x => x.o.SemesterId == request.SemesterId)
            .ToList();

        // ── Step 5: Build one StudentGradeRowDto per course ───────────────────
        var rows = semesterGradeRows.Select(x =>
        {
            var (grade, offering) = x;

            // Look up the course for code, name, credit hours
            var course = courses.FirstOrDefault(c => c.Id == offering.CourseId);

            // Sum all quiz scores for this grade record
            var quizTotal = quizGrades
                .Where(q => q.CourseGradeId == grade.Id)
                .Sum(q => q.Score);

            // Sum all discussion scores for this grade record
            var discTotal = discGrades
                .Where(d => d.CourseGradeId == grade.Id)
                .Sum(d => d.Score);

            // Run the BNU formula: coursework subtotal + grand total
            var (cw, total) = GradeCalculator.Calculate(
                grade.Midterm1Score, grade.Midterm2Score,
                discTotal, grade.AttendanceScore, quizTotal,
                grade.FinalExamScore);

            // VISIBILITY RULE: student only sees computed values when
            // the professor has published AND a final score exists.
            // Otherwise null → frontend shows "Pending".
            var canShow = grade.IsPublished && grade.FinalExamScore.HasValue;
            var letter = canShow ? GradeCalculator.GetLetterGrade(total) : null;
            var gpa = canShow ? GradeCalculator.GetGpaPoints(letter!) : (decimal?)null;

            // Use { } initializer — StudentGradeRowDto has NO positional constructor
            return new StudentGradeRowDto
            {
                CourseGradeId = grade.Id,
                CourseCode = course?.Code ?? string.Empty,
                CourseName = course?.Name ?? string.Empty,
                CreditHours = course?.CreditHours ?? 0,
                Total = canShow ? total : null,
                LetterGrade = letter,
                GpaPoints = gpa,
                IsPublished = grade.IsPublished,
                HasAcademicWarning = grade.HasAcademicWarning,

                // CourseBreakdownDto property names confirmed from repo:
                // DiscussionScore (not DiscussionTotal), QuizScore (not QuizTotal),
                // CourseWorkTotal (not CourseWork)
                Breakdown = new CourseBreakdownDto
                (
                    grade.Midterm1Score,
                    grade.Midterm2Score,
                    discTotal,
                    grade.AttendanceScore,
                    grade.AttendanceOverridden,
                    quizTotal,
                    canShow ? cw : null,
                    grade.FinalExamScore,
                    canShow ? total : null
                )
            };
        }).ToList();

        // ── Step 6: Cumulative GPA across ALL semesters (not just current) ────
        var allPublishedForCumulative = allGrades
            .Where(g => myEnrollmentIds.Contains(g.EnrollmentId)
                        && g.IsPublished
                        && g.FinalExamScore.HasValue)
            .Join(enrollments, g => g.EnrollmentId, e => e.Id, (g, e) => (g, e))
            .Join(sections, x => x.e.CourseSectionId, s => s.Id, (x, s) => (x.g, s))
            .Join(offerings, x => x.s.CourseOfferingId, o => o.Id, (x, o) => (x.g, o))
            .Join(courses, x => x.o.CourseId, c => c.Id, (x, c) =>
            (
                Total: GradeCalculator.Calculate(
                    x.g.Midterm1Score, x.g.Midterm2Score,
                    discGrades.Where(d => d.CourseGradeId == x.g.Id).Sum(d => d.Score),
                    x.g.AttendanceScore,
                    quizGrades.Where(q => q.CourseGradeId == x.g.Id).Sum(q => q.Score),
                    x.g.FinalExamScore).Total,
                c.CreditHours
            ))
            .ToList();

        var cumulativeGpa = GradeCalculator.CalculateGpa(allPublishedForCumulative);
        var totalCredits = allPublishedForCumulative.Sum(x => x.CreditHours);

        // ── Step 7: Semester GPA (this semester only) ─────────────────────────
        // Filter from the already-built rows — no extra DB call needed
        var semesterGpa = GradeCalculator.CalculateGpa(
            rows.Where(r => r.IsPublished && r.Total.HasValue)
                .Select(r => (r.Total!.Value, r.CreditHours)));

        // ── Step 8: Highest letter grade this semester (for UI badge) ─────────
        var highestGrade = rows
            .Where(r => r.GpaPoints.HasValue)
            .OrderByDescending(r => r.GpaPoints)
            .FirstOrDefault()?.LetterGrade;

        return Result<StudentSemesterSummaryDto>.Ok(new StudentSemesterSummaryDto(
            semester.Id,
            semester.Name,
            rows.Count,
            totalCredits,
            cumulativeGpa,
            highestGrade, // ← was missing/duplicated before
            rows.Count(r => r.IsPublished),
            rows 
        ));
    }
}
