// FILE: Features/Grades/TA/Queries/GetTaSectionGradesQueryHandler.cs
// PURPOSE: Load the full grade list for a TA's section.
//          Returns attendance, quiz total, and discussion total per student.
//          Does NOT expose midterm or final exam scores — those are professor-only.
// NOTE:    Student.Name used directly — AppUser is IdentityUser, cannot go through UoW.
//          StudentNationalId set to string.Empty — TA does not need it.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Grades;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Queries;

public class GetTaSectionGradesQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetTaSectionGradesQuery, Result<TaSectionGradesDto>>
{
    public async Task<Result<TaSectionGradesDto>> Handle(
        GetTaSectionGradesQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve TA from JWT AppUserId ─────────────────────────────────
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == request.CallerAppUserId);
        if (ta is null)
            return Result<TaSectionGradesDto>.Fail(
                Error.NotFound("Grades.TaNotFound", "Teaching Assistant profile not found."));

        // ── Step 2: Load all required tables ─────────────────────────────────────
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings   = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var courses     = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var semesters   = await _uow.GetRepository<Semester, Guid>().GetAllAsync();
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var allGrades   = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var students    = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var quizGrades  = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        var discGrades  = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();

        // ── Step 3: Validate section exists AND is assigned to this TA ────────────
        // Security check: a TA must not be able to query another TA's section.
        var section = sections.FirstOrDefault(s =>
            s.Id == request.SectionId && s.TeachingAssistantId == ta.Id);
        if (section is null)
            return Result<TaSectionGradesDto>.Fail(
                Error.NotFound("Grades.SectionNotFound", "Section not found or not assigned to you."));

        // ── Step 4: Resolve course and semester for response display info ─────────
        var offering = offerings.FirstOrDefault(o => o.Id == section.CourseOfferingId);
        var course   = courses.FirstOrDefault(c => c.Id == offering?.CourseId);
        var semester = semesters.FirstOrDefault(s => s.Id == offering?.SemesterId);

        // ── Step 5: Get every enrollment in this section ──────────────────────────
        var sectionEnrollments = enrollments
            .Where(e => e.CourseSectionId == section.Id)
            .ToList();
        var enrollmentIds = sectionEnrollments.Select(e => e.Id).ToHashSet();

        // ── Step 6: Get grade records for those enrollments ───────────────────────
        var sectionGrades = allGrades
            .Where(g => enrollmentIds.Contains(g.EnrollmentId))
            .ToList();

        // ── Step 7: Build one TaGradeRowDto per grade record ──────────────────────
        var studentRows = sectionGrades.Select(grade =>
        {
            var enrollment = sectionEnrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
            var student    = students.FirstOrDefault(s => s.Id == enrollment?.StudentId);

            // Sum all quiz child records for this CourseGrade
            var quizTotal = quizGrades
                .Where(q => q.CourseGradeId == grade.Id)
                .Sum(q => q.Score);

            // Sum all discussion child records for this CourseGrade
            var discTotal = discGrades
                .Where(d => d.CourseGradeId == grade.Id)
                .Sum(d => d.Score);

            return new TaGradeRowDto
            {
                CourseGradeId        = grade.Id,
                StudentId            = student?.Id ?? Guid.Empty,
                StudentName          = student?.Name ?? string.Empty, // Student.Name — no AppUser needed
                StudentNationalId    = string.Empty,                  // TA does not see NationalId
                AttendanceScore      = grade.AttendanceScore,
                AttendanceOverridden = grade.AttendanceOverridden,
                QuizTotal            = quizTotal,
                DiscussionTotal      = discTotal,
                HasAcademicWarning   = grade.HasAcademicWarning,
                ProfNote             = grade.ProfNote
            };
        }).ToList();

        return Result<TaSectionGradesDto>.Ok(new TaSectionGradesDto
        {
            SectionId    = section.Id,
            SectionName  = section.SectionName,
            CourseCode   = course?.Code   ?? string.Empty,
            CourseName   = course?.Name   ?? string.Empty,
            SemesterName = semester?.Name ?? string.Empty,
            StudentCount = studentRows.Count,
            Students     = studentRows
        });
    }
}
