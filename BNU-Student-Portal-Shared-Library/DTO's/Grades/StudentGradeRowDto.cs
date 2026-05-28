using BNU_Student_Portal_Shared_Library.DTO_s.Courses;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Grades;


public record StudentGradeRowDto
{
    public Guid    CourseGradeId { get; init; }
    public string  CourseCode    { get; init; } = default!;
    public string  CourseName    { get; init; } = default!;
    public int     CreditHours   { get; init; }
    public decimal? Total        { get; init; }   // null if not published
    public string?  LetterGrade  { get; init; }
    public decimal? GpaPoints    { get; init; }
    public bool    IsPublished   { get; init; }
    public bool    HasAcademicWarning { get; init; }
    public CourseBreakdownDto Breakdown { get; init; } = default!;
}