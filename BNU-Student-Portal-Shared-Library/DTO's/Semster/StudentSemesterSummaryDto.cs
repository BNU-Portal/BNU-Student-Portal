using BNU_Student_Portal_Shared_Library.DTO_s.Grades;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Semster;

public record StudentSemesterSummaryDto(
    Guid SemesterId,
    string SemesterName,
    decimal? SemesterGpa,
    int TotalCreditHours,
    decimal? CumulativeGpa,
    string? HighestGrade,
    int CurrentSemesterCourseCount,
    IEnumerable<StudentGradeRowDto> Grades
);