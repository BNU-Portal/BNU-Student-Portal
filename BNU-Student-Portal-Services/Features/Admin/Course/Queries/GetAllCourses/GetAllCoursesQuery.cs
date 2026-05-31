// FILE: Features/Admin/Course/Queries/GetAllCourses/GetAllCoursesQuery.cs
// PURPOSE: Returns all courses ordered alphabetically by Code.
// Used by the admin to pick a CourseId when creating a CourseOffering.

using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Course.Queries.GetAllCourses;

public record GetAllCoursesQuery : IRequest<Result<List<CourseDto>>>;
