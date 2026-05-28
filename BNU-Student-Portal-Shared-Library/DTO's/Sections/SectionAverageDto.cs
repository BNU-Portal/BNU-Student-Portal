namespace BNU_Student_Portal_Shared_Library.DTO_s.Sections;

public record SectionAverageDto
{
    public Guid     SectionId   { get; init; }
    public string   SectionName { get; init; } = default!;
    public decimal? Average     { get; init; }
}