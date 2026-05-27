using BNU_Student_Portal_Domain.Entities.Courses;

namespace BNU_Student_Portal_Domain.Entities.Semesters;

public class Semester : BaseEntity<Guid> 
{
    public string Name { get; set; } = default!;       // e.g. "Second Term 2025-2026"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    // adding the collection of CourseOfferings =>> as a semster has many courses => presented as sections and lectures 
    public ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();
}