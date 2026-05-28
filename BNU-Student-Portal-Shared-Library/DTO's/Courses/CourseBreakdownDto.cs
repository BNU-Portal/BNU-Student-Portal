namespace BNU_Student_Portal_Shared_Library.DTO_s.Courses;

public record CourseBreakdownDto
(
    decimal? Midterm1Score , // max 15
    decimal? Midterm2Score , // max 15
    decimal DiscussionScore , // sum of all discussions /15
    decimal AttendanceScore ,// /5
    bool AttendanceOverridden,
    decimal QuizScore , // sum of all quizzes /10
    decimal? CourseWorkTotal,// /60
    decimal? FinalExamScore , // /40
    decimal? Total  // /100
);