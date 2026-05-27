using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;

namespace BNU_Student_Portal_Domain.Entities.Sections;

///<summary>
///The join table between a Student and a CourseSection.
/// One enrollment = one student is registered in one section.
///
///WHY a separate enrollment entity (not just a FK on CourseGrade)?
///   An enrollment must exist before any grades exist.
///   The enrollment represents the administrative fact "this student is in this section".
///   CourseGrade represents the academic outcome — it can be null until grades are entered.
///  CourseGrade? is nullable here because a student can be enrolled without having
///grades entered yet (e.g. first day of semester).
///</summary>
public class StudentSectionEnrollment : BaseEntity<Guid>
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = default!;

    public Guid CourseSectionId { get; set; }
    public CourseSection CourseSection { get; set; } = default!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // One-to-one: each enrollment has at most one grade record.
    // Nullable because grades are created separately after enrollment.
    public CourseGrade? CourseGrade { get; set; }
}