using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Discussions;
using BNU_Student_Portal_Domain.Entities.Quizzes;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;

namespace BNU_Student_Portal_Domain.Entities.Courses;

public class CourseSection : BaseEntity<Guid>
{
    public Guid CourseOfferingId { get; set; }
    public CourseOffering CourseOffering { get; set; } = default!;

    // Copied from offering.SemesterId at creation time. Immutable after creation.
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = default!;

    public string SectionName { get; set; } = default!;  // e.g. "Section 1"

    // Maximum number of students allowed to self-enroll. Default: 25.
    // Admin enrollment bypasses this cap.
    public int MaxStudents { get; set; } = 25;

    public Guid TeachingAssistantId { get; set; }
    public TeachingAssistant TeachingAssistant { get; set; } = default!;

    public ICollection<StudentSectionEnrollment> Enrollments { get; set; } = new List<StudentSectionEnrollment>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
}
