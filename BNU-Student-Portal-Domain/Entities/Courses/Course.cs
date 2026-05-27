namespace BNU_Student_Portal_Domain.Entities.Courses;

public class Course : BaseEntity<Guid>
{
    public string Code { get; set; } = default!;       // e.g. "SAD304"
    public string Name { get; set; } = default!;       // e.g. "Machine Learning"
    public int CreditHours { get; set; }

    public ICollection<CourseOffering> Offerings { get; set; } = new List<CourseOffering>();
}