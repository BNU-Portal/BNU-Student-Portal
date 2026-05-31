// FILE: Features/Grades/TA/Commands/CreateDiscussion/CreateDiscussionCommandHandler.cs
// PURPOSE: Handles discussion creation + per-student DiscussionGrade seeding.
//
// FLOW:
//   1. Resolve TA from CallerAppUserId
//   2. Load all sections — find the one matching SectionId and verify TA owns it
//   3. Validate MaxScore > 0
//   4. Create Discussion row via UoW
//   5. Load all enrollments — filter to this section
//   6. Load all CourseGrades — filter by enrollment IDs
//   7. Seed one DiscussionGrade (Score=0) per student
//   8. SaveChangesAsync
//   9. Return Ok with DiscussionId + list of { CourseGradeId, DiscussionGradeId }

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Discussions;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateDiscussion;

public class CreateDiscussionCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<CreateDiscussionCommand, Result>
{
    public async Task<Result> Handle(CreateDiscussionCommand cmd, CancellationToken ct)
    {
        // 1. Resolve TA
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == cmd.CallerAppUserId);
        if (ta is null)
            return Result<object>.Fail(Error.NotFound("Grades.TaNotFound", "Teaching assistant not found."));

        // 2. Load section and verify TA owns it
        var sections = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var section  = sections.FirstOrDefault(s => s.Id == cmd.SectionId);
        if (section is null)
            return Result<object>.Fail(Error.NotFound("Grades.SectionNotFound", "Section not found."));
        if (section.TeachingAssistantId != ta.Id)
            return Result<object>.Fail(Error.Forbidden("Grades.Forbidden", "You are not assigned to this section."));

        // 3. Validate MaxScore
        if (cmd.MaxScore <= 0)
            return Result<object>.Fail(Error.BadRequest("Grades.InvalidMaxScore", "MaxScore must be greater than zero."));

        // 4. Create Discussion
        var discussion = new Discussion
        {
            Id              = Guid.NewGuid(),
            CourseSectionId = cmd.SectionId,
            Title           = cmd.Title.Trim(),
            MaxScore        = cmd.MaxScore
        };
        await _uow.GetRepository<Discussion, Guid>().AddAsync(discussion);

        // 5. Load enrollments and filter to this section
        var enrollments        = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sectionEnrollments = enrollments.Where(e => e.CourseSectionId == cmd.SectionId).ToList();
        if (sectionEnrollments.Count == 0)
            return Result<object>.Fail(Error.BadRequest("Grades.NoStudents", "No enrolled students found in this section."));

        // 6. Load CourseGrades and match by EnrollmentId
        var allGrades     = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var enrollmentIds = sectionEnrollments.Select(e => e.Id).ToHashSet();
        var courseGrades  = allGrades.Where(g => enrollmentIds.Contains(g.EnrollmentId)).ToList();

        // 7. Seed one DiscussionGrade per student (Score = 0)
        var discussionGrades = courseGrades.Select(cg => new DiscussionGrade
        {
            Id            = Guid.NewGuid(),
            DiscussionId  = discussion.Id,
            CourseGradeId = cg.Id,
            Score         = 0,
            Note          = null
        }).ToList();

        foreach (var dg in discussionGrades)
            await _uow.GetRepository<DiscussionGrade, Guid>().AddAsync(dg);

        // 8. Save everything in one transaction
        await _uow.SaveChangesAsync();

        // 9. Return DiscussionId + per-student DiscussionGradeIds for use in C5 EnterCoursework
        var studentEntries = courseGrades
            .Zip(discussionGrades, (cg, dg) => new
            {
                CourseGradeId     = cg.Id,
                DiscussionGradeId = dg.Id
            })
            .ToList();

        return Result<object>.Ok(new
        {
            DiscussionId = discussion.Id,
            Title        = discussion.Title,
            MaxScore     = discussion.MaxScore,
            Students     = studentEntries
        });
    }
}
