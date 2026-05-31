// FILE: Features/Student/Enrollment/Commands/SelfEnroll/SelfEnrollCommandHandler.cs
// PURPOSE: Handles student self-enrollment with capacity enforcement.
//
// IMPLEMENTATION FLOW:
// 1) Resolve Student by AppUserId. Return 404 if not found.
// 2) Validate CourseSection exists. Return 404 if not found.
// 3) Check section capacity: count current enrollments vs MaxStudents.
//    Return 400 SectionFull if at or over cap.
// 4) Prevent duplicate enrollment.
// 5) Create StudentSectionEnrollment.
// 6) Create blank CourseGrade (zeros, unpublished).
// 7) Atomic save — return EnrollmentId + CourseGradeId.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Student.Enrollment.Commands.SelfEnroll;

public class SelfEnrollCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<SelfEnrollCommand, Result<SelfEnrollResult>>
{
    public async Task<Result<SelfEnrollResult>> Handle(
        SelfEnrollCommand request, CancellationToken ct)
    {
        // 1) Resolve student by AppUserId
        var students = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Student, Guid>().GetAllAsync();
        var student  = students.FirstOrDefault(s => s.AppUserId == request.StudentAppUserId);
        if (student is null)
            return Result<SelfEnrollResult>.Fail(
                Error.NotFound("Enrollment.StudentNotFound",
                    $"Student with AppUserId '{request.StudentAppUserId}' not found."));

        // 2) Validate section exists
        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var section  = sections.FirstOrDefault(s => s.Id == request.CourseSectionId);
        if (section is null)
            return Result<SelfEnrollResult>.Fail(
                Error.NotFound("Enrollment.SectionNotFound",
                    $"CourseSection {request.CourseSectionId} not found."));

        // 3) Capacity check (admin flow skips this — only student self-enrollment enforces it)
        var enrollments  = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var currentCount = enrollments.Count(e => e.CourseSectionId == request.CourseSectionId);
        if (currentCount >= section.MaxStudents)
            return Result<SelfEnrollResult>.Fail(
                Error.BadRequest("Enrollment.SectionFull",
                    $"Section '{section.SectionName}' is full ({section.MaxStudents}/{section.MaxStudents} students)."));

        // 4) Duplicate guard
        if (enrollments.Any(e =>
            e.StudentId       == student.Id &&
            e.CourseSectionId == request.CourseSectionId))
            return Result<SelfEnrollResult>.Fail(
                Error.Validation("Enrollment.Duplicate",
                    "You are already enrolled in this section."));

        // 5) Create enrollment
        var enrollment = new StudentSectionEnrollment
        {
            Id              = Guid.NewGuid(),
            StudentId       = student.Id,
            CourseSectionId = request.CourseSectionId,
            EnrolledAt      = DateTime.UtcNow
        };
        await _uow.GetRepository<StudentSectionEnrollment, Guid>().AddAsync(enrollment);

        // 6) Create blank grade row
        var grade = new CourseGrade
        {
            Id                 = Guid.NewGuid(),
            EnrollmentId       = enrollment.Id,
            AttendanceScore    = 0,
            IsPublished        = false,
            HasAcademicWarning = false
        };
        await _uow.GetRepository<CourseGrade, Guid>().AddAsync(grade);

        // 7) Atomic save
        await _uow.SaveChangesAsync();

        return Result<SelfEnrollResult>.Ok(new SelfEnrollResult(enrollment.Id, grade.Id));
    }
}
