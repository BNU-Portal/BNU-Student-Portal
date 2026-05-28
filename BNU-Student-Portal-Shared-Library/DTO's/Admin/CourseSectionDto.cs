namespace BNU_Student_Portal_Shared_Library.DTO_s.Admin;

public record CourseSectionDto
{
    public Guid   Id                  { get; init; }
    public string SectionName         { get; init; } = default!;
    public string CourseOfferingInfo  { get; init; } = default!;  // "CourseCode - SemesterName"
    public string TaName              { get; init; } = default!;
}
