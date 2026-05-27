using BNU_Student_Portal_Domain.Entities.Quizzes;

namespace BNU_Student_Portal_Domain.Entities.Grades;

public class QuizGrade : BaseEntity<Guid>
{
    public Guid CourseGradeId { get; set; }
    public CourseGrade CourseGrade { get; set; } = default!;

    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;

    public decimal Score { get; set; }
    public string? Note { get; set; }
}