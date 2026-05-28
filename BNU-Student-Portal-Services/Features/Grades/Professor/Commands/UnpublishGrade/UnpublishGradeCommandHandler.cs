// FILE: Features/Grades/Professor/Commands/UnpublishGrade/UnpublishGradeCommandHandler.cs
// PURPOSE: Verify professor owns the grade, check it is currently published,
//          then set IsPublished = false.
// NOTE:    CourseGrade is a class — mutate directly, no 'with'.

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Commands.UnpublishGrade;

public class UnpublishGradeCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<UnpublishGradeCommand, Result>
{
    public async Task<Result> Handle(UnpublishGradeCommand request, CancellationToken ct)
    {
        // ── Step 1: Resolve professor ──────────────────────────────────────────────
        var professors = await _uow.GetRepository<Professor, Guid>().GetAllAsync();
        var professor  = professors.FirstOrDefault(p => p.AppUserId == request.CallerAppUserId);
        if (professor is null)
            return Result.Fail(Error.NotFound("Grades.ProfessorNotFound", "Professor not found."));

        // ── Step 2: Load the target grade record ───────────────────────────────────
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result.Fail(Error.NotFound("Grades.GradeNotFound", "Grade record not found."));

        // ── Step 3: Ownership check — walk grade → enrollment → section → offering ─
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings   = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();

        var enrollment = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        var section    = sections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);
        var offering   = offerings.FirstOrDefault(o => o.Id == section?.CourseOfferingId);

        if (offering?.ProfessorId != professor.Id)
            return Result.Fail(Error.Forbidden("Grades.Forbidden",
                "This grade does not belong to your course."));

        // ── Step 4: Guard — nothing to do if already unpublished ──────────────────
        if (!grade.IsPublished)
            return Result.Fail(Error.BadRequest("Grades.NotPublished",
                "This grade is already unpublished."));

        // ── Step 5: Unpublish and persist ──────────────────────────────────────────
        grade.IsPublished = false;
        await _uow.GetRepository<CourseGrade, Guid>().UpdateAsync(grade);
        await _uow.SaveChangesAsync();

        return Result.Ok();
    }
}
