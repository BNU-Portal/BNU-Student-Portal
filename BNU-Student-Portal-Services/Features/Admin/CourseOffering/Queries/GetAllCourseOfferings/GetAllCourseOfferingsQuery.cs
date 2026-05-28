// FILE: Features/Admin/CourseOffering/Queries/GetAllCourseOfferings/GetAllCourseOfferingsQuery.cs
// PURPOSE: Admin lists all offerings with course, semester, and professor names.
//
// FLOW DIAGRAM:
// [GET /api/admin/course-offerings]
//    -> [GetAllCourseOfferingsQuery]
//    -> [GetAllCourseOfferingsQueryHandler]
//         | load offerings + lookup course/semester/professor
//         | map to CourseOfferingDto
//    -> returns CourseOfferingDto list

using BNU_Student_Portal_Shared_Library.DTO_s.Admin;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Queries.GetAllCourseOfferings;

public record GetAllCourseOfferingsQuery : IRequest<Result<List<CourseOfferingDto>>>;
