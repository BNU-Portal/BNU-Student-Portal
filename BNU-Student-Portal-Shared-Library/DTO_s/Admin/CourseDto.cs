// FILE: Shared/DTO_s/Admin/CourseDto.cs
// PURPOSE: Response shape returned by GET /api/admin/courses

namespace BNU_Student_Portal_Shared_Library.DTO_s.Admin;

public class CourseDto
{
    public Guid   Id          { get; set; }
    public string Code        { get; set; } = default!;
    public string Name        { get; set; } = default!;
    public int    CreditHours { get; set; }
}
