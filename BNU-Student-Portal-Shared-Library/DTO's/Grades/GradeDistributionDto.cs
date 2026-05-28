using BNU_Student_Portal_Shared_Library.DTO_s.Sections;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Grades;

public record GradeDistributionDto
{
    public int      TotalStudents  { get; init; }
    public int      ACount         { get; init; }
    public int      BCount         { get; init; }
    public int      CCount         { get; init; }
    public int      DCount         { get; init; }
    public int      FCount         { get; init; }
    public int      NotGradedCount { get; init; }
    public decimal? ClassAverage   { get; init; }
    public IEnumerable<SectionAverageDto> SectionAverages { get; init; } = [];
}
