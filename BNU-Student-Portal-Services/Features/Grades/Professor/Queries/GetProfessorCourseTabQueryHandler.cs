using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Courses;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Queries;

public class GetProfessorCourseTabQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetProfessorCourseTabQuery, Result<IEnumerable<ProfessorCourseTabDto>>>
{
    // FLOW SUMMARY:
    // Resolve professor -> filter offerings -> aggregate sections/enrollments/grades
    // -> compute publish/pending status -> return tab DTOs
    //
    // DIAGRAM:
    // Professor(AppUserId)
    //   -> Offerings -> Sections -> Enrollments -> CourseGrades
    //   -> ProfessorCourseTabDto
    public async Task<Result<IEnumerable<ProfessorCourseTabDto>>> Handle(
        GetProfessorCourseTabQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve professor from JWT AppUserId ──────────────────────
        var professors = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Professor, Guid>()
            .GetAllAsync();
        var professor = professors.FirstOrDefault(p => p.AppUserId == request.CallerAppUserId);
        if (professor is null)
            return Result<IEnumerable<ProfessorCourseTabDto>>.Fail(
                Error.NotFound("Grades.ProfessorNotFound", "Professor profile not found."));

        // ── Step 2: Load all required tables into memory ──────────────────────
        var offerings = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var courses = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();

        // ── Step 3: Filter to only this professor's offerings ─────────────────
        var myOfferings = offerings
            .Where(o => o.ProfessorId == professor.Id)
            .ToList();

        // ── Step 4: Build one tab per offering ────────────────────────────────
        var tabs = myOfferings.Select(offering =>
        {
            var course = courses.FirstOrDefault(c => c.Id == offering.CourseId);

            // Find every section that belongs to this offering
            var sectionIds = sections
                .Where(s => s.CourseOfferingId == offering.Id)
                .Select(s => s.Id)
                .ToHashSet();

            // Find every enrollment in those sections
            var enrollmentIds = enrollments
                .Where(e => sectionIds.Contains(e.CourseSectionId))
                .Select(e => e.Id)
                .ToHashSet();

            // Find every grade record for those enrollments
            var courseGrades = allGrades
                .Where(g => enrollmentIds.Contains(g.EnrollmentId))
                .ToList();

            // AllPublished = every student has been published (and at least 1 exists)
            var allPublished = courseGrades.Count > 0 && courseGrades.All(g => g.IsPublished);

            // PendingCount = students who have no FinalExamScore yet
            var pendingCount = courseGrades.Count(g => !g.FinalExamScore.HasValue);

            return new ProfessorCourseTabDto
            {
                CourseOfferingId = offering.Id,
                CourseCode = course?.Code ?? string.Empty,
                CourseName = course?.Name ?? string.Empty,
                AllPublished = allPublished,
                PendingCount = pendingCount
            };
        }).ToList();

        return Result<IEnumerable<ProfessorCourseTabDto>>.Ok(tabs);
    }
}
