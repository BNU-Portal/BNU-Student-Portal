using BNU_Student_Portal_Domain.Entities.Discussions;

namespace BNU_Student_Portal_Domain.Entities.Grades;

public class DiscussionGrade : BaseEntity<Guid>
{
    public Guid        CourseGradeId { get; set; }
    public CourseGrade CourseGrade   { get; set; } = default!;

    public Guid       DiscussionId { get; set; }
    public Discussion Discussion   { get; set; } = default!;

    public decimal Score { get; set; }
    public string? Note  { get; set; }
}