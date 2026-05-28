// FILE: Features/Admin/Enrollment/Commands/EnrollStudent/EnrollStudentCommandHandler.cs
// PURPOSE: Creates a StudentSectionEnrollment + blank CourseGrade in one transaction.
//          This is the minimal setup required before any Grades endpoint can be tested.

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
        var students = await _uow.GetRepository<Student, Guid>().GetAllAsync();
        if (!students.Any(s => s.Id == request.StudentId))
            return Result<EnrollmentResult>.Fail(
                Error.NotFound("Enrollment.StudentNotFound",
                    $"Student {request.StudentId} not found."));

        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        if (!sections.Any(s => s.Id == request.CourseSectionId))
            return Result<EnrollmentResult>.Fail(
                Error.NotFound("Enrollment.SectionNotFound",
                    $"CourseSection {request.CourseSectionId} not found."));

        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        if (enrollments.Any(e =>
            e.StudentId       == request.StudentId &&
            e.CourseSectionId == request.CourseSectionId))
            return Result<EnrollmentResult>.Fail(
                Error.Validation("Enrollment.Duplicate",
                    "Student is already enrolled in this section."));

        var enrollment = new StudentSectionEnrollment
        {
            Id              = Guid.NewGuid(),
            StudentId       = request.StudentId,
            CourseSectionId = request.CourseSectionId,
            EnrolledAt      = DateTime.UtcNow
        };
        await _uow.GetRepository<StudentSectionEnrollment, Guid>().AddAsync(enrollment);

        var grade = new CourseGrade
        {
            Id                 = Guid.NewGuid(),
            EnrollmentId       = enrollment.Id,
            AttendanceScore    = 0,
            IsPublished        = false,
            HasAcademicWarning = false
        };
        await _uow.GetRepository<CourseGrade, Guid>().AddAsync(grade);

        await _uow.SaveChangesAsync();

        return Result<EnrollmentResult>.Ok(new EnrollmentResult(enrollment.Id, grade.Id));
    }
}
