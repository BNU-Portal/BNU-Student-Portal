namespace BNU_Student_Portal_Services.Features.Grades;

public static class GradeCalculator
{
    /* 
     * FLOW DIAGRAM (Full Grade Calculation Pipeline):
     * 
     *   [Input Raw Scores] ---------------------------------------+
     *          |                                                  |
     *          v                                                  |
     *   +-------------------------+                               |
     *   | Calculate()             |                               |
     *   |-------------------------|                               |
     *   | CW = Mid1 + Mid2 + Disc |                               |
     *   |      + Att + Quiz       |                               |
     *   | Total = CW + Final      |                               |
     *   +-------------------------+                               |
     *          |                                                  |
     *          v                                                  |
     *   +-------------------------+      +---------------------+  |
     *   | GetLetterGrade()        | <--- | Business Rules (Max)|--+
     *   |-------------------------|      +---------------------+
     *   | 90+ -> A+               |
     *   | ...                     |
     *   | <40 -> F                |
     *   +-------------------------+
     *          |
     *          v
     *   +-------------------------+
     *   | GetGpaPoints()          |
     *   |-------------------------|
     *   | A+ -> 4.0               |
     *   | ...                     |
     *   +-------------------------+
     *          |
     *          v
     *   +-------------------------+
     *   | CalculateGpa()          |
     *   |-------------------------|
     *   | Sum(Points * Credits) / |
     *   | Total Credits           |
     *   +-------------------------+
     *          |
     *          v
     *      [Final GPA]
     */

    // BNU business rules — locked
    public const decimal MidtermMax    = 30m;   // 15 + 15
    public const decimal DiscussionMax = 15m;
    public const decimal AttendanceMax = 5m;
    public const decimal QuizMax       = 10m;
    public const decimal CourseWorkMax = 60m;   // 30+15+5+10
    public const decimal FinalMax      = 40m;
    public const decimal GrandTotal    = 100m;

    public static (decimal CourseWork, decimal Total) Calculate(
        decimal? mid1,
        decimal? mid2,
        decimal  discussionTotal,
        decimal  attendanceScore,
        decimal  quizTotal,
        decimal? finalExam)
    {
        var cw    = (mid1 ?? 0) + (mid2 ?? 0)
                  + discussionTotal + attendanceScore + quizTotal;
        var total = cw + (finalExam ?? 0);
        return (Math.Round(cw, 2), Math.Round(total, 2));
    }

    public static string GetLetterGrade(decimal total) => total switch
    {
        >= 90 => "A+",
        >= 85 => "A",
        >= 80 => "A-",
        >= 75 => "B+",
        >= 70 => "B",
        >= 65 => "B-",
        >= 60 => "C+",
        >= 55 => "C",
        >= 50 => "C-",
        >= 45 => "D+",
        >= 40 => "D",
        _     => "F"
    };

    public static decimal GetGpaPoints(string letter) => letter switch
    {
        "A+" or "A" => 4.0m,
        "A-"        => 3.7m,
        "B+"        => 3.3m,
        "B"         => 3.0m,
        "B-"        => 2.7m,
        "C+"        => 2.3m,
        "C"         => 2.0m,
        "C-"        => 1.7m,
        "D+"        => 1.3m,
        "D"         => 1.0m,
        _           => 0.0m
    };

    public static decimal? CalculateGpa(
        IEnumerable<(decimal Total, int CreditHours)> publishedCourses)
    {
        var list = publishedCourses.ToList();
        if (list.Count == 0) return null;
        var points  = list.Sum(c => GetGpaPoints(GetLetterGrade(c.Total)) * c.CreditHours);
        var credits = list.Sum(c => c.CreditHours);
        return credits > 0 ? Math.Round(points / credits, 2) : null;
    }

    public static string GetBucket(decimal total) => total switch
    {
        >= 85 => "A",
        >= 75 => "B",
        >= 65 => "C",
        >= 50 => "D",
        _     => "F"
    };
}
