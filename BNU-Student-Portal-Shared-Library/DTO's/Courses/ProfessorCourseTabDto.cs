namespace BNU_Student_Portal_Shared_Library.DTO_s.Courses;

public record ProfessorCourseTabDto
{
    public Guid CourseOfferingId { get; init; }
    public string CourseCode { get; init; } = default!;
    public string CourseName { get; init; } = default!;
    public bool AllPublished { get; init; }
    public int PendingCount { get; init; } // students without FinalExamScore
}