using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Semesters;

namespace BNU_Student_Portal_Domain.Entities.Courses;

public class CourseOffering : BaseEntity<Guid>
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = default!;

    public Guid ProfessorId { get; set; }
    public Professor Professor { get; set; } = default!;

    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
}