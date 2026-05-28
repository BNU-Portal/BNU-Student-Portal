// FILE: Features/Grades/Professor/Queries/GetProfessorCourseGradesQueryHandler.cs
// PURPOSE: Build the full grade sheet + distribution for one course offering.
// FIX:     Removed broken GetRepository<AppUser, string>() call — AppUser extends
//          IdentityUser, not BaseEntity<string>, so it cannot go through UoW.
//          Student.Name is used directly (it exists on the Student entity).
//          StudentNationalId is set to string.Empty — professor does not need it.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Services.Features.Grades.Professor.Queries;
using BNU_Student_Portal_Shared_Library.DTO_s.Grades;
using BNU_Student_Portal_Shared_Library.DTO_s.Sections;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Queries;

public class GetProfessorCourseGradesQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetProfessorCourseGradesQuery, Result<ProfessorCourseGradesDto>>
{
    public async Task<Result<ProfessorCourseGradesDto>> Handle(
        GetProfessorCourseGradesQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve professor from the AppUserId stored in the JWT claim ──
        var professors = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Professor, Guid>().GetAllAsync();
        var professor  = professors.FirstOrDefault(p => p.AppUserId == request.CallerAppUserId);
        if (professor is null)
            return Result<ProfessorCourseGradesDto>.Fail(
                Error.NotFound("Grades.ProfessorNotFound", "Professor profile not found."));

        // ── Step 2: Validate the offering exists AND belongs to this professor ────
        var offerings = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var offering  = offerings.FirstOrDefault(o =>
            o.Id == request.CourseOfferingId && o.ProfessorId == professor.Id);
        if (offering is null)
            return Result<ProfessorCourseGradesDto>.Fail(
                Error.NotFound("Grades.OfferingNotFound", "Course offering not found or not yours."));

        // ── Step 3: Load all required tables into memory ─────────────────────────
        // NOTE: No AppUser table — AppUser extends IdentityUser, not BaseEntity<T>.
        //       Student.Name is used directly for display names.
        var courses     = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var semesters   = await _uow.GetRepository<Semester, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var allGrades   = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var students    = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var quizGrades  = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        var discGrades  = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();

        var course   = courses.FirstOrDefault(c => c.Id == offering.CourseId);
        var semester = semesters.FirstOrDefault(s => s.Id == offering.SemesterId);

        // ── Step 4: Get all sections that belong to this offering ─────────────────
        var offeringSections   = sections.Where(s => s.CourseOfferingId == offering.Id).ToList();
        var offeringSectionIds = offeringSections.Select(s => s.Id).ToHashSet();

        // ── Step 5: Get all enrollments in those sections ─────────────────────────
        var offeringEnrollments = enrollments
            .Where(e => offeringSectionIds.Contains(e.CourseSectionId))
            .ToList();
        var enrollmentIds = offeringEnrollments.Select(e => e.Id).ToHashSet();

        // ── Step 6: Get all grade records for those enrollments ───────────────────
        var courseGrades = allGrades
            .Where(g => enrollmentIds.Contains(g.EnrollmentId))
            .ToList();

        // ── Step 7: Build one ProfessorGradeRowDto per CourseGrade record ─────────
        var studentRows = courseGrades.Select(grade =>
        {
            // Walk back the chain: grade → enrollment → section → student
            var enrollment = offeringEnrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
            var section    = offeringSections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);
            var student    = students.FirstOrDefault(s => s.Id == enrollment?.StudentId);

            // Sum all child quiz scores for this grade record
            var quizTotal = quizGrades
                .Where(q => q.CourseGradeId == grade.Id)
                .Sum(q => q.Score);

            // Sum all child discussion scores for this grade record
            var discTotal = discGrades
                .Where(d => d.CourseGradeId == grade.Id)
                .Sum(d => d.Score);

            // Run the BNU scoring formula to get CourseWork subtotal and grand Total
            var (cw, total) = GradeCalculator.Calculate(
                grade.Midterm1Score, grade.Midterm2Score,
                discTotal, grade.AttendanceScore, quizTotal,
                grade.FinalExamScore);

            // Totals are only meaningful once a FinalExamScore exists
            var hasTotal = grade.FinalExamScore.HasValue;
            var letter   = hasTotal ? GradeCalculator.GetLetterGrade(total) : null;

            return new ProfessorGradeRowDto
            {
                CourseGradeId        = grade.Id,
                StudentId            = student?.Id ?? Guid.Empty,
                StudentName          = student?.Name ?? string.Empty, // Student.Name — no AppUser needed
                StudentNationalId    = string.Empty,                  // Professor does not see NationalId
                SectionId            = section?.Id ?? Guid.Empty,
                SectionName          = section?.SectionName ?? string.Empty, // confirmed: prop is SectionName
                Midterm1Score        = grade.Midterm1Score,
                Midterm2Score        = grade.Midterm2Score,
                AttendanceScore      = grade.AttendanceScore,
                AttendanceOverridden = grade.AttendanceOverridden,
                FinalExamScore       = grade.FinalExamScore,
                CourseWorkScore      = hasTotal ? cw    : null,
                Total                = hasTotal ? total : null,
                LetterGrade          = letter,
                IsPublished          = grade.IsPublished,
                HasAcademicWarning   = grade.HasAcademicWarning,
                ProfNote             = grade.ProfNote
            };
        }).ToList();

        // ── Step 8: Build grade distribution stats for the entire offering ─────────
        var gradedRows = studentRows.Where(r => r.LetterGrade is not null).ToList();

        // Per-section average: only count students who have a computed Total
        var sectionAverages = offeringSections.Select(sec =>
        {
            var secStudents = studentRows
                .Where(r => r.SectionId == sec.Id && r.Total.HasValue)
                .ToList();
            var avg = secStudents.Count > 0
                ? secStudents.Average(r => r.Total!.Value)
                : (decimal?)null;
            return new SectionAverageDto
            {
                SectionId   = sec.Id,
                SectionName = sec.SectionName,
                Average     = avg
            };
        }).ToList();

        var classAverage = gradedRows.Count > 0
            ? gradedRows.Average(r => r.Total!.Value)
            : (decimal?)null;

        var distribution = new GradeDistributionDto
        {
            TotalStudents   = studentRows.Count,
            ACount          = gradedRows.Count(r => r.LetterGrade == "A" || r.LetterGrade == "A+"),
            BCount          = gradedRows.Count(r => r.LetterGrade!.StartsWith("B")),
            CCount          = gradedRows.Count(r => r.LetterGrade!.StartsWith("C")),
            DCount          = gradedRows.Count(r => r.LetterGrade!.StartsWith("D")),
            FCount          = gradedRows.Count(r => r.LetterGrade == "F"),
            NotGradedCount  = studentRows.Count - gradedRows.Count,
            ClassAverage    = classAverage,
            SectionAverages = sectionAverages
        };

        // ── Step 9: Assemble and return the full response DTO ─────────────────────
        return Result<ProfessorCourseGradesDto>.Ok(new ProfessorCourseGradesDto
        {
            CourseOfferingId = offering.Id,
            CourseCode       = course?.Code   ?? string.Empty,
            CourseName       = course?.Name   ?? string.Empty,
            SemesterName     = semester?.Name ?? string.Empty,
            TotalStudents    = studentRows.Count,
            SectionCount     = offeringSections.Count,
            AllPublished     = studentRows.Count > 0 && studentRows.All(r => r.IsPublished),
            Distribution     = distribution,
            Students         = studentRows
        });
    }
}
