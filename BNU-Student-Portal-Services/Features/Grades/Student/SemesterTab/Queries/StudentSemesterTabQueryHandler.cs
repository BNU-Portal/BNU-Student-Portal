using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Semster;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.SemesterTab.Queries;

public class StudentSemesterTabQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<StudentSemesterTabQuery, Result<IEnumerable<SemesterTabDto>>>
{
    // FLOW SUMMARY:
    // Resolve student -> walk enrollments -> derive semester IDs -> map to tabs
    //
    // DIAGRAM:
    // Student(AppUserId)
    //   -> Enrollments
    //      -> Sections
    //         -> Offerings
    //            -> Semesters -> SemesterTabDto
    public async Task<Result<IEnumerable<SemesterTabDto>>> Handle(
        StudentSemesterTabQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve student from JWT AppUserId ───────────────────────
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student = students.FirstOrDefault(s => s.AppUserId == request.CallerId);
        if (student is null)
            return Result<IEnumerable<SemesterTabDto>>.Fail(
                Error.NotFound("Grades.StudentNotFound", "Student profile not found."));

        // ── Step 2: Load all required tables into memory ─────────────────────
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var semesters = await _uow.GetRepository<Semester, Guid>().GetAllAsync();

        // ── Step 3: Walk the chain to find which semesters this student is in ─
        // enrollment → section → offering → semester
        var semesterIds = enrollments
            .Where(e => e.StudentId == student.Id)
            .Join(sections, e => e.CourseSectionId, s => s.Id, (e, s) => s)
            .Join(offerings, s => s.CourseOfferingId, o => o.Id, (s, o) => o)
            .Select(o => o.SemesterId)
            .ToHashSet(); // deduplicate

        // ── Step 4: Build tab list, sorted newest first ───────────────────────
        var tabs = semesters
            .Where(s => semesterIds.Contains(s.Id))
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SemesterTabDto
            {
                SemesterId = s.Id,
                SemesterName = s.Name,
                IsActive = s.IsActive
            });

        return Result<IEnumerable<SemesterTabDto>>.Ok(tabs);
    }
}
