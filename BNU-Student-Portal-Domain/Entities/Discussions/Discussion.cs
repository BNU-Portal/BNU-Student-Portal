using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;

namespace BNU_Student_Portal_Domain.Entities.Discussions;

// Identical pattern to Quiz — a discussion activity defined at section level.
// MaxScore is variable, set when created.
public class Discussion : BaseEntity<Guid>
{
    public Guid          CourseSectionId { get; set; }
    public CourseSection CourseSection   { get; set; } = default!;

    public string  Title    { get; set; } = default!;
    public decimal MaxScore { get; set; }

    public ICollection<DiscussionGrade> Grades { get; set; } = new List<DiscussionGrade>(); 
}