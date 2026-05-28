// FILE: Features/Grades/TA/Commands/EnterCoursework/EnterCourseworkCommand.cs
// PURPOSE: TA enters or updates quiz and discussion scores for one student.
//          Each quiz/discussion is a separate child record (QuizGrade/DiscussionGrade)
//          identified by its own ID — the TA updates existing records, not creates them.
//          (QuizGrade and DiscussionGrade rows are seeded when the section is created.)

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.EnterCoursework;

// One item per quiz in the section
public record QuizScoreItem(Guid QuizGradeId, decimal Score);

// One item per discussion in the section
public record DiscussionScoreItem(Guid DiscussionGradeId, decimal Score);

public record EnterCourseworkCommand(
    string  CallerAppUserId,
    Guid    CourseGradeId,                           // the parent grade record
    IEnumerable<QuizScoreItem>       QuizScores,     // can be empty if no quizzes
    IEnumerable<DiscussionScoreItem> DiscussionScores) // can be empty if no discussions
    : IRequest<Result>;
