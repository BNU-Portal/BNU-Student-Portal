using BNU_Student_Portal_Shared_Library.DTO_s.Grades;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Semster;

public record StudentSemesterSummaryDto
{
    public decimal? CumulativeGpa { get; init; }
    public int TotalCreditHours { get; init; }
    public string? HighestGrade { get; init; }
    public int CurrentSemesterCourseCount { get; init; }
    public Guid SemesterId { get; init; }
    public string SemesterName { get; init; } = default!;
    public decimal? SemesterGpa { get; init; }
    public IEnumerable<StudentGradeRowDto> Grades { get; init; } = [];
}