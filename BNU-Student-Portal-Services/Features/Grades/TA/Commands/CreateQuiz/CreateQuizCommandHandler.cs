// FILE: Features/Grades/TA/Commands/CreateQuiz/CreateQuizCommandHandler.cs
// PURPOSE: Handles quiz creation + per-student QuizGrade seeding.
//
// FLOW:
//   1. Resolve TA from CallerAppUserId
//   2. Load all sections — find the one matching SectionId and verify TA owns it
//   3. Validate MaxScore > 0
//   4. Create Quiz row via UoW
//   5. Load all CourseGrades — filter by EnrollmentId chain to find students in section
//   6. Load all enrollments to resolve which grades belong to this section
//   7. Seed one QuizGrade (Score=0) per student
//   8. SaveChangesAsync
//   9. Return Ok with QuizId + list of { StudentName, CourseGradeId, QuizGradeId }

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Quizzes;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateQuiz;

public class CreateQuizCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<CreateQuizCommand, Result>
{
    public async Task<Result> Handle(CreateQuizCommand cmd, CancellationToken ct)
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

        // 4. Create Quiz
        var quiz = new Quiz
        {
            Id              = Guid.NewGuid(),
            CourseSectionId = cmd.SectionId,
            Title           = cmd.Title.Trim(),
            MaxScore        = cmd.MaxScore
        };
        await _uow.GetRepository<Quiz, Guid>().AddAsync(quiz);

        // 5. Load enrollments to find students in this section
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sectionEnrollments = enrollments.Where(e => e.CourseSectionId == cmd.SectionId).ToList();
        if (sectionEnrollments.Count == 0)
            return Result<object>.Fail(Error.BadRequest("Grades.NoStudents", "No enrolled students found in this section."));

        // 6. Load CourseGrades and match by EnrollmentId
        var allGrades    = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var enrollmentIds = sectionEnrollments.Select(e => e.Id).ToHashSet();
        var courseGrades  = allGrades.Where(g => enrollmentIds.Contains(g.EnrollmentId)).ToList();

        // 7. Seed one QuizGrade per student (Score = 0)
        var quizGrades = courseGrades.Select(cg => new QuizGrade
        {
            Id            = Guid.NewGuid(),
            QuizId        = quiz.Id,
            CourseGradeId = cg.Id,
            Score         = 0,
            Note          = null
        }).ToList();

        foreach (var qg in quizGrades)
            await _uow.GetRepository<QuizGrade, Guid>().AddAsync(qg);

        // 8. Save everything in one transaction
        await _uow.SaveChangesAsync();

        // 9. Return QuizId + per-student QuizGradeIds for use in C5 EnterCoursework
        var studentEntries = courseGrades
            .Zip(quizGrades, (cg, qg) => new
            {
                CourseGradeId = cg.Id,
                QuizGradeId   = qg.Id
            })
            .ToList();

        return Result<object>.Ok(new
        {
            QuizId   = quiz.Id,
            Title    = quiz.Title,
            MaxScore = quiz.MaxScore,
            Students = studentEntries
        });
    }
}
