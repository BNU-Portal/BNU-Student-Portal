// FILE: Presentation/controllers/GradesController.cs

using BNU_Student_Portal_Services.Features.Grades.Professor.Commands.EnterGrade;
using BNU_Student_Portal_Services.Features.Grades.Professor.Commands.PublishGrades;
using BNU_Student_Portal_Services.Features.Grades.Professor.Commands.UnpublishGrade;
using BNU_Student_Portal_Services.Features.Grades.Professor.Queries;
using BNU_Student_Portal_Services.Features.Grades.Student.GradesPerSemester.Queries;
using BNU_Student_Portal_Services.Features.Grades.Student.SemesterTab.Queries;
using BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateDiscussion;
using BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateQuiz;
using BNU_Student_Portal_Services.Features.Grades.TA.Commands.EnterCoursework;
using BNU_Student_Portal_Services.Features.Grades.TA.Commands.UpdateAttendance;
using BNU_Student_Portal_Services.Features.Grades.TA.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BNU_Student_Portal_Presentation.Controllers;

// ── Request DTOs (body only — no CallerAppUserId) ──────────────────────────────

public record EnterGradeRequest(
    Guid     CourseGradeId,
    decimal? Midterm1Score,
    decimal? Midterm2Score,
    decimal? FinalExamScore,
    string?  ProfNote);

public record UpdateAttendanceRequest(
    Guid    CourseGradeId,
    decimal AttendanceScore,
    bool    AttendanceOverridden);

public record EnterCourseworkRequest(
    Guid                              CourseGradeId,
    IEnumerable<QuizScoreItem>        QuizScores,
    IEnumerable<DiscussionScoreItem>  DiscussionScores);

public record CreateQuizRequest(
    Guid    SectionId,
    string  Title,
    decimal MaxScore);

public record CreateDiscussionRequest(
    Guid    SectionId,
    string  Title,
    decimal MaxScore);

// ── Controller ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/grades")]
[Authorize]
public class GradesController(ISender _sender) : ApiBaseController
{
    // ════════════════════════════════════════════════════════════════════
    // STUDENT ENDPOINTS
    // ════════════════════════════════════════════════════════════════════

    [HttpGet("student/semesters")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentSemesterTabs()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new StudentSemesterTabQuery(callerId));
        return HandleResult(result);
    }

    [HttpGet("student/semesters/{semesterId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentGradesBySemester(Guid semesterId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetStudentGradesBySemesterQuery(callerId, semesterId));
        return HandleResult(result);
    }

    // ════════════════════════════════════════════════════════════════════
    // PROFESSOR ENDPOINTS
    // ════════════════════════════════════════════════════════════════════

    [HttpGet("professor/courses")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> GetProfessorCourseTabs()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetProfessorCourseTabQuery(callerId));
        return HandleResult(result);
    }

    [HttpGet("professor/courses/{courseOfferingId:guid}")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> GetProfessorCourseGrades(Guid courseOfferingId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetProfessorCourseGradesQuery(callerId, courseOfferingId));
        return HandleResult(result);
    }

    [HttpPut("professor/grade")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> EnterGrade([FromBody] EnterGradeRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command  = new EnterGradeCommand(
            callerId,
            request.CourseGradeId,
            request.Midterm1Score,
            request.Midterm2Score,
            request.FinalExamScore,
            request.ProfNote);
        var result = await _sender.Send(command);
        return HandleResult(result);
    }

    [HttpPost("professor/courses/{courseOfferingId:guid}/publish")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> PublishGrades(Guid courseOfferingId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new PublishGradesCommand(callerId, courseOfferingId));
        return HandleResult(result);
    }

    [HttpPost("professor/grade/{courseGradeId:guid}/unpublish")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> UnpublishGrade(Guid courseGradeId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new UnpublishGradeCommand(callerId, courseGradeId));
        return HandleResult(result);
    }

    // ════════════════════════════════════════════════════════════════════
    // TEACHING ASSISTANT ENDPOINTS
    // ════════════════════════════════════════════════════════════════════

    [HttpGet("ta/section")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> GetTaSectionTab()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetTaSectionTabQuery(callerId));
        return HandleResult(result);
    }

    [HttpGet("ta/section/{sectionId:guid}")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> GetTaSectionGrades(Guid sectionId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetTaSectionGradesQuery(callerId, sectionId));
        return HandleResult(result);
    }

    [HttpPut("ta/attendance")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> UpdateAttendance([FromBody] UpdateAttendanceRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command  = new UpdateAttendanceCommand(
            callerId,
            request.CourseGradeId,
            request.AttendanceScore,
            request.AttendanceOverridden);
        var result = await _sender.Send(command);
        return HandleResult(result);
    }

    [HttpPut("ta/coursework")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> EnterCoursework([FromBody] EnterCourseworkRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command  = new EnterCourseworkCommand(
            callerId,
            request.CourseGradeId,
            request.QuizScores,
            request.DiscussionScores);
        var result = await _sender.Send(command);
        return HandleResult(result);
    }

    // C6 — returns Result<CreateQuizResponse> so HandleResult<T> picks up the body
    [HttpPost("ta/quizzes")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command  = new CreateQuizCommand(
            callerId,
            request.SectionId,
            request.Title,
            request.MaxScore);
        var result = await _sender.Send(command);
        return HandleResult(result);   // result is Result<CreateQuizResponse> → generic overload ✓
    }

    // C7 — returns Result<CreateDiscussionResponse> so HandleResult<T> picks up the body
    [HttpPost("ta/discussions")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> CreateDiscussion([FromBody] CreateDiscussionRequest request)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command  = new CreateDiscussionCommand(
            callerId,
            request.SectionId,
            request.Title,
            request.MaxScore);
        var result = await _sender.Send(command);  // result is Result<CreateDiscussionResponse> → generic overload ✓
        return HandleResult(result);
    }
}
