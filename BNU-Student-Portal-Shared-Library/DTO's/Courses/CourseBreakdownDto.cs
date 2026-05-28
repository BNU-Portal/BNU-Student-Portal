namespace BNU_Student_Portal_Shared_Library.DTO_s.Courses;

public record CourseBreakdownDto
{
    public decimal? Midterm1Score { get; init; } // max 15
    public decimal? Midterm2Score { get; init; } // max 15
    public decimal DiscussionScore { get; init; } // sum of all discussions /15
    public decimal AttendanceScore { get; init; } // /5
    public bool AttendanceOverridden { get; init; }
    public decimal QuizScore { get; init; } // sum of all quizzes /10
    public decimal? CourseWorkTotal { get; init; } // /60
    public decimal? FinalExamScore { get; init; } // /40
    public decimal? Total { get; init; } // /100
}