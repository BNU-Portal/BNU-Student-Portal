// FILE: Presentation/controllers/AdminController.cs
// PURPOSE: Admin-only endpoints to set up test data for the Grades feature.
//
// ROUTE DESIGN:
//   POST   /api/admin/semesters                     -> Create semester
//   GET    /api/admin/semesters                     -> List all semesters
//   GET    /api/admin/semesters/active              -> Get active semester
//   PUT    /api/admin/semesters/{id}/activate       -> Activate a semester
//   POST   /api/admin/course-offerings              -> Create course offering
//   GET    /api/admin/course-offerings              -> List all course offerings
//   POST   /api/admin/course-sections               -> Create section under offering
//   POST   /api/admin/enrollments                   -> Enroll student in section

// SETUP FLOW (recommended order for Grades data):
// 1) CreateSemester
// 2) ActivateSemester
// 3) CreateCourseOffering
// 4) CreateCourseSection
// 5) EnrollStudent
//
// FLOW DIAGRAM (high level):
// [Admin Client]
//    |
//    v
// [AdminController] -> [MediatR] -> [Feature Handler] -> [UoW/Repo] -> [DB]
//    ^--------------------------------------------------------------------|
//    Result<T> returns to caller with IDs for the next step

using BNU_Student_Portal_Services.Features.Admin.CourseOffering.Commands.CreateCourseOffering;
using BNU_Student_Portal_Services.Features.Admin.CourseOffering.Queries.GetAllCourseOfferings;
using BNU_Student_Portal_Services.Features.Admin.CourseSection.Commands.CreateCourseSection;
using BNU_Student_Portal_Services.Features.Admin.Enrollment.Commands.EnrollStudent;
using BNU_Student_Portal_Services.Features.Admin.Semester.Commands.ActivateSemester;
using BNU_Student_Portal_Services.Features.Admin.Semester.Commands.CreateSemester;
using BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetActiveSemester;
using BNU_Student_Portal_Services.Features.Admin.Semester.Queries.GetAllSemesters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BNU_Student_Portal_Presentation.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(ISender _sender) : ApiBaseController
{
    // ================================================================
    // SEMESTER
    // ================================================================

    /// <summary>
    /// Creates a new semester. IsActive defaults to false.
    /// Call the activate endpoint afterwards to make it the current semester.
    /// </summary>
    [HttpPost("semesters")]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterCommand command)
    {
        var result = await _sender.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Returns all semesters ordered by StartDate descending.
    /// </summary>
    [HttpGet("semesters")]
    public async Task<IActionResult> GetAllSemesters()
    {
        var result = await _sender.Send(new GetAllSemestersQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Returns the currently active semester.
    /// 404 if no semester has been activated yet.
    /// </summary>
    [HttpGet("semesters/active")]
    public async Task<IActionResult> GetActiveSemester()
    {
        var result = await _sender.Send(new GetActiveSemesterQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Marks one semester as active and deactivates all others.
    /// Only one semester can be active at a time.
    /// </summary>
    [HttpPut("semesters/{semesterId:guid}/activate")]
    public async Task<IActionResult> ActivateSemester(Guid semesterId)
    {
        var result = await _sender.Send(new ActivateSemesterCommand(semesterId));
        return HandleResult(result);
    }

    // ================================================================
    // COURSE OFFERING
    // ================================================================

    /// <summary>
    /// Links a Course + Semester + Professor into one CourseOffering.
    /// Duplicate combinations (same course+semester+professor) are rejected.
    /// </summary>
    [HttpPost("course-offerings")]
    public async Task<IActionResult> CreateCourseOffering([FromBody] CreateCourseOfferingCommand command)
    {
        var result = await _sender.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Returns all course offerings with their Course, Semester and Professor info.
    /// Use the returned Id values when creating sections.
    /// </summary>
    [HttpGet("course-offerings")]
    public async Task<IActionResult> GetAllCourseOfferings()
    {
        var result = await _sender.Send(new GetAllCourseOfferingsQuery());
        return HandleResult(result);
    }

    // ================================================================
    // COURSE SECTION
    // ================================================================

    /// <summary>
    /// Creates a section under a CourseOffering and assigns a TA.
    /// SemesterId is automatically copied from the CourseOffering.
    /// </summary>
    [HttpPost("course-sections")]
    public async Task<IActionResult> CreateCourseSection([FromBody] CreateCourseSectionCommand command)
    {
        var result = await _sender.Send(command);
        return HandleResult(result);
    }

    // ================================================================
    // ENROLLMENT
    // ================================================================

    /// <summary>
    /// Enrolls a student into a section and creates a blank CourseGrade row.
    /// Returns both the EnrollmentId and the CourseGradeId for reference.
    /// After this call, all Grades endpoints (Q3-Q6, C1-C6) will have data to work with.
    /// </summary>
    [HttpPost("enrollments")]
    public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentCommand command)
    {
        var result = await _sender.Send(command);
        return HandleResult(result);
    }
}
