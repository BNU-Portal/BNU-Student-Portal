// FILE: Features/Grades/Student/CourseDetail/Queries/CourseGradeDetailDto.cs
// PURPOSE: Full per-course detail returned to the student.

namespace BNU_Student_Portal_Services.Features.Grades.Student.CourseDetail.Queries;

// Top-level response
public record CourseGradeDetailDto(
    Guid     CourseGradeId,
    string   CourseCode,
    string   CourseName,
    int      CreditHours,

    // Exam scores (always visible — not gated by IsPublished)
    decimal? Midterm1Score,
    decimal? Midterm2Score,
    decimal? FinalExamScore,

    // Attendance
    decimal  AttendanceScore,
    bool     AttendanceOverridden,

    // Per-item breakdowns
    IEnumerable<QuizDetailItem>       Quizzes,
    IEnumerable<DiscussionDetailItem> Discussions,

    // Professor note on the whole course
    string?  ProfNote,

    // Computed totals — null when not published yet
    decimal? CourseWorkTotal,
    decimal? Total,
    string?  LetterGrade,
    decimal? GpaPoints,

    bool     IsPublished,
    bool     HasAcademicWarning
);

// One quiz entry
public record QuizDetailItem(
    string   Title,
    decimal  Score,
    decimal  MaxScore,
    string?  Note        // TA note per quiz grade
);

// One discussion entry
public record DiscussionDetailItem(
    string   Title,
    decimal  Score,
    decimal  MaxScore,
    string?  Note        // TA note per discussion grade
);
