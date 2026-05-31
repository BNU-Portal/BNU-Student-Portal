// FILE: Features/Grades/TA/Commands/CreateQuiz/CreateQuizResponse.cs
// PURPOSE: Response DTO returned from C6 CreateQuiz.
//          Contains the new QuizId + one entry per student so the TA
//          knows which QuizGradeId to pass into C5 EnterCoursework.

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateQuiz;

public record CreateQuizResponse(
    Guid   QuizId,
    string Title,
    decimal MaxScore,
    IEnumerable<QuizStudentEntry> Students);

public record QuizStudentEntry(
    Guid CourseGradeId,
    Guid QuizGradeId);
