// FILE: Features/Grades/TA/Commands/UpdateAttendance/UpdateAttendanceCommandHandler.cs
// PURPOSE: Resolve TA, verify the grade belongs to their section, check it is not
//          published, then update AttendanceScore and AttendanceOverridden.
// NOTE:    CourseGrade is a class — mutate directly, no 'with'.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.UpdateAttendance;

public class UpdateAttendanceCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<UpdateAttendanceCommand, Result>
{
    public async Task<Result> Handle(UpdateAttendanceCommand request, CancellationToken ct)
    {
        // ── Step 1: Resolve TA from JWT AppUserId ─────────────────────────────────
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == request.CallerAppUserId);
        if (ta is null)
            return Result<object>.Fail(Error.NotFound("Grades.TaNotFound", "Teaching Assistant not found."));

        // ── Step 2: Load the grade record ─────────────────────────────────────────
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result<object>.Fail(Error.NotFound("Grades.GradeNotFound", "Grade record not found."));

        // ── Step 3: Verify grade belongs to a section assigned to this TA ─────────
        // Walk: grade → enrollment → section, then check section.TeachingAssistantId
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();

        var enrollment = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        var section    = sections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);

        if (section?.TeachingAssistantId != ta.Id)
            return Result<object>.Fail(Error.Forbidden("Grades.Forbidden",
                "This student is not in your section."));

        // ── Step 4: Block edits on published grades ────────────────────────────────
        if (grade.IsPublished)
            return Result<object>.Fail(Error.BadRequest("Grades.AlreadyPublished",
                "Grade is published. Ask the professor to unpublish it before editing."));

        // ── Step 5: Apply attendance update and persist ────────────────────────────
        grade.AttendanceScore      = request.AttendanceScore;
        grade.AttendanceOverridden = request.AttendanceOverridden;

        _uow.GetRepository<CourseGrade, Guid>().Update(grade);
        await _uow.SaveChangesAsync();

        return Result<object>.Ok("Attendance updated successfully.");
    }
}
