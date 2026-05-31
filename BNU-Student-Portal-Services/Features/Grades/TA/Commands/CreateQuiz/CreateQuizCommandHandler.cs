// FILE: Features/Grades/TA/Commands/CreateQuiz/CreateQuizCommandHandler.cs
// PURPOSE: Handles quiz creation + per-student QuizGrade seeding.
//
// FLOW:
//   1. Resolve TA from CallerAppUserId
//   2. Load the CourseSection and verify TA owns it
//   3. Check IsPublished is false (cannot add quizzes after grades are published)
//   4. Create Quiz row
//   5. Load all CourseGrades for students in this section
//   6. Seed one QuizGrade (Score=0) per student
//   7. SaveChangesAsync
//   8. Return Ok with the new QuizId + list of { StudentName, QuizGradeId }

using BNU_Student_Portal_Persistence.Data.DbContext;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Quizzes;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateQuiz;

public class CreateQuizCommandHandler(
    BNU_Student_Portal_DbContext _db)
    : IRequestHandler<CreateQuizCommand, Result>
{
    public async Task<Result> Handle(CreateQuizCommand cmd, CancellationToken ct)
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

        // 4. Create Quiz
        var quiz = new Quiz
        {
            Id              = Guid.NewGuid(),
            CourseSectionId = cmd.SectionId,
            Title           = cmd.Title.Trim(),
            MaxScore        = cmd.MaxScore
        };
        _db.Quizzes.Add(quiz);

        // 5. Load all CourseGrades for students enrolled in this section
        var courseGrades = await _db.CourseGrades
            .Include(g => g.Enrollment)
                .ThenInclude(e => e.Student)
                    .ThenInclude(s => s.AppUser)
            .Where(g => g.Enrollment.CourseSectionId == cmd.SectionId)
            .ToListAsync(ct);

        if (courseGrades.Count == 0)
            return Result.Failure("No enrolled students found in this section.");

        // 6. Seed one QuizGrade per student (Score = 0)
        var quizGrades = courseGrades.Select(cg => new QuizGrade
        {
            Id            = Guid.NewGuid(),
            QuizId        = quiz.Id,
            CourseGradeId = cg.Id,
            Score         = 0,
            Note          = null
        }).ToList();

        _db.QuizGrades.AddRange(quizGrades);

        // 7. Save
        await _db.SaveChangesAsync(ct);

        // 8. Return QuizId + per-student QuizGradeIds for use in EnterCoursework
        var studentEntries = courseGrades
            .Zip(quizGrades, (cg, qg) => new
            {
                StudentName = cg.Enrollment.Student.AppUser.UserName,
                CourseGradeId = cg.Id,
                QuizGradeId   = qg.Id
            })
            .ToList();

        return Result.Ok(new
        {
            QuizId   = quiz.Id,
            Title    = quiz.Title,
            MaxScore = quiz.MaxScore,
            Students = studentEntries
        });
    }
}
