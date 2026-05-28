namespace BNU_Student_Portal_Shared_Library.DTO_s.Grades;

public record ProfessorGradeRowDto
{
    public Guid CourseGradeId { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = default!;
    public string StudentNationalId { get; init; } = default!; // from AppUser.NationalId
    public Guid SectionId { get; init; }
    public string SectionName { get; init; } = default!;

    // Editable components
    public decimal? Midterm1Score { get; init; }
    public decimal? Midterm2Score { get; init; }
    public decimal AttendanceScore { get; init; }
    public bool AttendanceOverridden { get; init; }
    public decimal? FinalExamScore { get; init; }

    // Computed
    public decimal? CourseWorkScore { get; init; } // /60
    public decimal? Total { get; init; } // /100
    public string? LetterGrade { get; init; }

    // Flags
    public bool IsPublished { get; init; }
    public bool HasAcademicWarning { get; init; }
    public string? ProfNote { get; init; }
}