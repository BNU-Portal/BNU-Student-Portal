// FILE: Features/Admin/Enrollment/Commands/EnrollStudent/EnrollStudentCommandHandler.cs
// PURPOSE: Creates a StudentSectionEnrollment + blank CourseGrade in one transaction.
//
// IMPLEMENTATION FLOW:
// 1) Resolve Student by AppUserId (NOT Student.Id PK). Return 404 if not found.
// 2) Validate CourseSection exists.
// 3) Prevent duplicate enrollment for same student/section.
// 4) Create StudentSectionEnrollment.
// 5) Create CourseGrade with default values (scores zero, unpublished).
// 6) Save atomically and return both IDs.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Enrollment.Commands.EnrollStudent;

public class EnrollStudentCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<EnrollStudentCommand, Result<EnrollmentResult>>
{
    public async Task<Result<EnrollmentResult>> Handle(
        EnrollStudentCommand request, CancellationToken ct)
    {
        // 1) Resolve Student by AppUserId (not PK)
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student  = students.FirstOrDefault(s => s.AppUserId == request.StudentAppUserId);
        if (student is null)
            return Result<EnrollmentResult>.Fail(
                Error.NotFound("Enrollment.StudentNotFound",
                    $"Student with AppUserId '{request.StudentAppUserId}' not found."));

        // 2) Validate CourseSection
        var sections = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Courses.CourseSection, Guid>().GetAllAsync();
        if (!sections.Any(s => s.Id == request.CourseSectionId))
            return Result<EnrollmentResult>.Fail(
                Error.NotFound("Enrollment.SectionNotFound",
                    $"CourseSection {request.CourseSectionId} not found."));

        // 3) Duplicate guard
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        if (enrollments.Any(e =>
            e.StudentId       == student.Id &&
            e.CourseSectionId == request.CourseSectionId))
            return Result<EnrollmentResult>.Fail(
                Error.Validation("Enrollment.Duplicate",
                    "Student is already enrolled in this section."));

        // 4) Create enrollment
        var enrollment = new StudentSectionEnrollment
        {
            Id              = Guid.NewGuid(),
            StudentId       = student.Id,
            CourseSectionId = request.CourseSectionId,
            EnrolledAt      = DateTime.UtcNow
        };
        await _uow.GetRepository<StudentSectionEnrollment, Guid>().AddAsync(enrollment);

        // 5) Create blank grade row
        var grade = new CourseGrade
        {
            Id                 = Guid.NewGuid(),
            EnrollmentId       = enrollment.Id,
            AttendanceScore    = 0,
            IsPublished        = false,
            HasAcademicWarning = false
        };
        await _uow.GetRepository<CourseGrade, Guid>().AddAsync(grade);

        // 6) Atomic save
        await _uow.SaveChangesAsync();

        return Result<EnrollmentResult>.Ok(new EnrollmentResult(enrollment.Id, grade.Id));
    }
}
