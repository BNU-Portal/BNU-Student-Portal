using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Semster;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.SemesterTab.Queries;

public class StudentSemesterTabQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<StudentSemesterTabQuery, Result<IEnumerable<SemesterTabDto>>>
{
    public async Task<Result<IEnumerable<SemesterTabDto>>> Handle(StudentSemesterTabQuery request,
        CancellationToken cancellationToken)
    {
        var students = await unitOfWork.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>()
            .GetAllAsync();
        var student  = students.FirstOrDefault(s => s.AppUserId == request.CallerId);
        if (student is null)
            return Result<IEnumerable<SemesterTabDto>>.Fail(
                Error.NotFound("Grades.StudentNotFound", "Student profile not found."));
        
        var enrollments = await unitOfWork.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await unitOfWork.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings   = await unitOfWork.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var semesters   = await unitOfWork.GetRepository<Semester, Guid>().GetAllAsync();
        
        // AFTER (direct lookup — 1 line)
        var semesterIds = enrollments
            .Where(e => e.StudentId == student.Id)
            .Join(sections, e => e.CourseSectionId, s => s.Id, (_, s) => s.SemesterId)
            .Distinct().ToHashSet();
       
        var ordered = semesters
            .Where(s => semesterIds.Contains(s.Id))
            .OrderByDescending(s => s.StartDate)
            .ToList();
        
        // ── Step 5: Project to DTO ────────────────────────────────────────────
        // "IsActive = true" only for the first tab (index 0 = most recent semester).
        // The frontend uses this to pre-select the correct tab on page load.
        var tabs = ordered.Select((s, i) =>
            new SemesterTabDto
            {
                SemesterId = s.Id,
                SemesterName = s.Name,
                IsActive = i == 0
            });
        return Result<IEnumerable<SemesterTabDto>>.Ok(tabs);

        

    }
}