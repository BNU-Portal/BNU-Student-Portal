using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;

namespace BNU_Student_Portal_Domain.Entities.Quizzes;

// A Quiz is defined at the section level by a Professor or TA.
// MaxScore is set when the quiz is created — it varies per quiz.
// Each quiz can have many QuizGrade rows (one per student).
public class Quiz : BaseEntity<Guid>
{
    public Guid          CourseSectionId { get; set; }
    public CourseSection CourseSection   { get; set; } = default!;

    public string  Title    { get; set; } = default!;
    public decimal MaxScore { get; set; }

    public ICollection<QuizGrade> Grades { get; set; } = new List<QuizGrade>();
}