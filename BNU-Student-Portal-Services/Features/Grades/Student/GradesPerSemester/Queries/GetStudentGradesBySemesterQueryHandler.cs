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
        // ── Step 1: Resolve student and validate semester ────────────────────
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student = students.FirstOrDefault(s => s.AppUserId == request.CallerAppUserId);
        if (student is null)
            return Result<StudentSemesterSummaryDto>.Fail(
                Error.NotFound("Grades.StudentNotFound", "Student profile not found."));

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

        // ── Step 3: Get this student's enrollment IDs ─────────────────────────
        // We collect just the IDs into a HashSet so later lookups are O(1)
        var myEnrollmentIds = enrollments
            .Where(e => e.StudentId == student.Id)
            .Select(e => e.Id)
            .ToHashSet();

        // ── Step 4: Find grade records that are in the requested semester ─────
        // A CourseGrade links to an Enrollment, which links to a Section,
        // which links to an Offering, which has the SemesterId.
        // So we join 3 levels deep (same chain as the tabs query, but this time
        // we carry the grade object along and filter by semester at the end).
        var semesterGradeRows = allGrades
            // Only grades for THIS student's enrollments
            .Where(g => myEnrollmentIds.Contains(g.EnrollmentId))

            // Hop 1: grade → enrollment (to get CourseSectionId)
            .Join(enrollments, g => g.EnrollmentId, e => e.Id, (g, e) => (g, e))

            // Hop 2: enrollment → section (to get CourseOfferingId)
            .Join(sections, x => x.e.CourseSectionId, s => s.Id, (x, s) => (x.g, s))

            // Hop 3: section → offering (to check SemesterId)
            .Join(offerings, x => x.s.CourseOfferingId, o => o.Id, (x, o) => (x.g, o))

            // Filter: only grades from the requested semester
            .Where(x => x.o.SemesterId == request.SemesterId)
            .ToList();

        // ── Step 5: Build one grade row per course ────────────────────────────
        var rows = semesterGradeRows.Select(x =>
        {
            var (grade, offering) = x;

            // Look up the course for its code, name, and credit hours
            var course = courses.FirstOrDefault(c => c.Id == offering.CourseId);

            // Sum all quiz scores for this specific grade record
            // (a student may have multiple quiz rows, one per quiz taken)
            var quizTotal = quizGrades
                .Where(q => q.CourseGradeId == grade.Id)
                .Sum(q => q.Score);

            // Same for discussion grades
            var discTotal = discGrades
                .Where(d => d.CourseGradeId == grade.Id)
                .Sum(d => d.Score);

            // Run the calculator to get course-work subtotal and grand total
            var (cw, total) = GradeCalculator.Calculate(
                grade.Midterm1Score, grade.Midterm2Score,
                discTotal, grade.AttendanceScore, quizTotal,
                grade.FinalExamScore);

            // VISIBILITY RULE:
            // Only show the final total and letter grade to the student if:
            //   1. The professor has marked this grade as published (IsPublished == true)
            //   2. A final exam score has actually been entered
            // If either condition fails, we return null for Total/Letter/GpaPoints
            // so the frontend shows "—" or "Pending" instead of a wrong number.
            var canShow = grade.IsPublished && grade.FinalExamScore.HasValue;
            var letter = canShow ? GradeCalculator.GetLetterGrade(total) : null;
            var gpa = canShow ? GradeCalculator.GetGpaPoints(letter!) : (decimal?)null;

            return new StudentGradeRowDto(
                grade.Id,
                course?.Code ?? string.Empty,
                course?.Name ?? string.Empty,
                course?.CreditHours ?? 0,
                canShow ? total : null,
                letter,
                gpa,
                grade.IsPublished,
                grade.HasAcademicWarning,
                new CourseBreakdownDto(
                    grade.Midterm1Score,
                    grade.Midterm2Score,
                    discTotal,
                    grade.AttendanceScore,
                    grade.AttendanceOverridden,
                    quizTotal,
                    canShow ? cw : null, // hide until published
                    grade.FinalExamScore,
                    canShow ? total : null));
        }).ToList();

        // ── Step 6: Cumulative GPA (all semesters, not just current) ─────────
        // Cumulative GPA = weighted average across ALL published courses ever taken.
        // We re-query across all enrollments (not just this semester) to get it.
        var allPublishedForCumulative = allGrades
            .Where(g =>
                myEnrollmentIds.Contains(g.EnrollmentId) &&
                g.IsPublished && // must be published
                g.FinalExamScore.HasValue) // must have a final score
            .Join(enrollments, g => g.EnrollmentId, e => e.Id, (g, e) => (g, e))
            .Join(sections, x => x.e.CourseSectionId, s => s.Id, (x, s) => (x.g, s))
            .Join(offerings, x => x.s.CourseOfferingId, o => o.Id, (x, o) => (x.g, o))
            .Join(courses, x => x.o.CourseId, c => c.Id, (x, c) =>
            (
                // Compute total on-the-fly for each course to feed into GPA formula
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

        // ── Step 7: Semester GPA (current semester only) ─────────────────────
        // Filter from the already-built rows list — no need to re-query
        var semesterGpa = GradeCalculator.CalculateGpa(
            rows.Where(r => r.IsPublished && r.Total.HasValue)
                .Select(r => (r.Total!.Value, r.CreditHours)));

        // ── Step 8: Highest grade this semester (for display badge) ───────────
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
            highestGrade,
            totalCredits,
            rows));
    }
}