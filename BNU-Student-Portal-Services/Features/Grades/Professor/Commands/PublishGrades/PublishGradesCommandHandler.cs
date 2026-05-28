// FILE: Features/Grades/Professor/Commands/PublishGrades/PublishGradesCommandHandler.cs
// PURPOSE: Validate offering ownership, find all unpublished grades that have a
//          FinalExamScore, set IsPublished = true on each, then save once.
// NOTE:    CourseGrade is a class — mutate directly, no 'with'.
//
// FLOW SUMMARY:
// Resolve professor -> validate offering -> collect enrollmentIds -> filter grades
// -> mark published -> save
//
// DIAGRAM:
// Professor(AppUserId)
//   -> Offering -> Sections -> Enrollments -> CourseGrades
//   -> Publish -> Save

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Commands.PublishGrades;

public class PublishGradesCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<PublishGradesCommand, Result>
{
    public async Task<Result> Handle(PublishGradesCommand request, CancellationToken ct)
    {
        // ── Step 1: Resolve professor ──────────────────────────────────────────────
        var professors = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Professor, Guid>().GetAllAsync();
        var professor = professors.FirstOrDefault(p => p.AppUserId == request.CallerAppUserId);
        if (professor is null)
            return Result<object>.Fail(Error.NotFound("Grades.ProfessorNotFound", "Professor not found."));

        // ── Step 2: Validate offering belongs to this professor ────────────────────
        var offerings = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var offering = offerings.FirstOrDefault(o =>
            o.Id == request.CourseOfferingId && o.ProfessorId == professor.Id);
        if (offering is null)
            return Result<object>.Fail(Error.NotFound("Grades.OfferingNotFound",
                "Course offering not found or does not belong to you."));

        // ── Step 3: Gather all enrollment IDs for this offering ────────────────────
        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();

        var sectionIds = sections
            .Where(s => s.CourseOfferingId == offering.Id)
            .Select(s => s.Id)
            .ToHashSet();

        var enrollmentIds = enrollments
            .Where(e => sectionIds.Contains(e.CourseSectionId))
            .Select(e => e.Id)
            .ToHashSet();

        // ── Step 4: Select only grades that are ready to publish ───────────────────
        // Ready = not already published + has a FinalExamScore
        var gradesToPublish = allGrades
            .Where(g => enrollmentIds.Contains(g.EnrollmentId)
                        && !g.IsPublished
                        && g.FinalExamScore.HasValue)
            .ToList();

        if (gradesToPublish.Count == 0)
            return Result<object>.Fail(Error.BadRequest("Grades.NothingToPublish",
                "No grades with a final exam score are ready to publish."));

        // ── Step 5: Mark each grade published and persist in one SaveChanges call ──
        foreach (var grade in gradesToPublish)
        {
            grade.IsPublished = true;
            _uow.GetRepository<CourseGrade, Guid>().Update(grade);
        }

        await _uow.SaveChangesAsync();
        return Result<object>.Ok("Grades published successfully.");
    }
}
