// FILE: Presentation/controllers/StudentController.cs
// PURPOSE: Student-facing endpoints. All endpoints require the Student role.
//
// ROUTE DESIGN:
//   POST /api/student/enrollments  -> Student self-enrolls into a section (cap enforced)
//
// NOTE: StudentAppUserId is ALWAYS extracted from the JWT token (ClaimTypes.NameIdentifier).
//       Students never pass their own ID in the request body.

using BNU_Student_Portal_Services.Features.Student.Enrollment.Commands.SelfEnroll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BNU_Student_Portal_Presentation.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentController(ISender _sender) : ApiBaseController
{
    // ================================================================
    // ENROLLMENT
    // ================================================================

    /// <summary>
    /// Student self-enrolls into a course section.
    /// Section capacity (MaxStudents) is enforced — returns 400 if section is full.
    /// A blank CourseGrade row is automatically created alongside the enrollment.
    /// Body: { "courseSectionId": "<guid>" }
    /// </summary>
    [HttpPost("enrollments")]
    public async Task<IActionResult> SelfEnroll([FromBody] SelfEnrollRequest request)
    {
        var studentAppUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentAppUserId))
            return Unauthorized();

        var command = new SelfEnrollCommand(studentAppUserId, request.CourseSectionId);
        var result  = await _sender.Send(command);
        return HandleResult(result);
    }
}

/// <summary>
/// Request body for POST /api/student/enrollments.
/// StudentAppUserId is NOT included — it is read from the JWT token.
/// </summary>
public record SelfEnrollRequest(Guid CourseSectionId);
