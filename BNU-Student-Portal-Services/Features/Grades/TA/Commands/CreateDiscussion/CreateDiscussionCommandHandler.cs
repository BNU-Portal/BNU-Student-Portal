// FILE: Features/Grades/TA/Commands/CreateDiscussion/CreateDiscussionCommandHandler.cs
// PURPOSE: Handles discussion creation + per-student DiscussionGrade seeding.
//
// FLOW:
//   1. Resolve TA from CallerAppUserId
//   2. Load the CourseSection and verify TA owns it
//   3. Validate MaxScore > 0
//   4. Create Discussion row
//   5. Load all CourseGrades for students in this section
//   6. Seed one DiscussionGrade (Score=0) per student
//   7. SaveChangesAsync
//   8. Return Ok with the new DiscussionId + list of { StudentName, DiscussionGradeId }

using BNU_Student_Portal_Persistence.Data.DbContext;
using BNU_Student_Portal_Domain.Entities.Discussions;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateDiscussion;

public class CreateDiscussionCommandHandler(
    BNU_Student_Portal_DbContext _db)
    : IRequestHandler<CreateDiscussionCommand, Result>
{
    public async Task<Result> Handle(CreateDiscussionCommand cmd, CancellationToken ct)
    {
        // 1. Resolve TA
        var ta = await _db.TeachingAssistants
            .FirstOrDefaultAsync(t => t.AppUserId == cmd.CallerAppUserId, ct);
        if (ta is null)
            return Result.Failure("Teaching assistant not found.");

        // 2. Load section and verify ownership
        var section = await _db.CourseSections
            .FirstOrDefaultAsync(s => s.Id == cmd.SectionId, ct);
        if (section is null)
            return Result.Failure("Section not found.");
        if (section.TeachingAssistantId != ta.Id)
            return Result.Failure("You are not assigned to this section.");

        // 3. Validate MaxScore
        if (cmd.MaxScore <= 0)
            return Result.Failure("MaxScore must be greater than zero.");

        // 4. Create Discussion
        var discussion = new Discussion
        {
            Id              = Guid.NewGuid(),
            CourseSectionId = cmd.SectionId,
            Title           = cmd.Title.Trim(),
            MaxScore        = cmd.MaxScore
        };
        _db.Discussions.Add(discussion);

        // 5. Load all CourseGrades for students enrolled in this section
        var courseGrades = await _db.CourseGrades
            .Include(g => g.Enrollment)
                .ThenInclude(e => e.Student)
                    .ThenInclude(s => s.AppUser)
            .Where(g => g.Enrollment.CourseSectionId == cmd.SectionId)
            .ToListAsync(ct);

        if (courseGrades.Count == 0)
            return Result.Failure("No enrolled students found in this section.");

        // 6. Seed one DiscussionGrade per student (Score = 0)
        var discussionGrades = courseGrades.Select(cg => new DiscussionGrade
        {
            Id             = Guid.NewGuid(),
            DiscussionId   = discussion.Id,
            CourseGradeId  = cg.Id,
            Score          = 0,
            Note           = null
        }).ToList();

        _db.DiscussionGrades.AddRange(discussionGrades);

        // 7. Save
        await _db.SaveChangesAsync(ct);

        // 8. Return DiscussionId + per-student DiscussionGradeIds for use in EnterCoursework
        var studentEntries = courseGrades
            .Zip(discussionGrades, (cg, dg) => new
            {
                StudentName        = cg.Enrollment.Student.AppUser.UserName,
                CourseGradeId      = cg.Id,
                DiscussionGradeId  = dg.Id
            })
            .ToList();

        return Result.Ok(new
        {
            DiscussionId = discussion.Id,
            Title        = discussion.Title,
            MaxScore     = discussion.MaxScore,
            Students     = studentEntries
        });
    }
}
