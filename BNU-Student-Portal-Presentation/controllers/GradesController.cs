// FILE: Presentation/controllers/GradesController.cs
// PURPOSE: Exposes all Grades endpoints for Student, Professor, and TA roles.
//
// SECURITY PATTERN: CallerAppUserId is ALWAYS extracted from the validated JWT
//   using User.FindFirstValue(ClaimTypes.NameIdentifier) inside each action.
//   It is NEVER read from the request body — this prevents impersonation.
//
//   For C1/C4/C5/C6/C7 (commands with CallerAppUserId), we use separate request DTOs
//   as [FromBody] instead of the command directly. This avoids ASP.NET model
//   binding rejecting the request with 400 "CallerAppUserId is required" before
//   the controller action even runs.
//
// ROUTE DESIGN:
//   GET  /api/grades/student/semesters              → Q1 semester tabs
//   GET  /api/grades/student/semesters/{id}         → Q2 grades per semester
//   GET  /api/grades/professor/courses              → Q3 course tabs
//   GET  /api/grades/professor/courses/{id}         → Q4 full grade sheet
//   PUT  /api/grades/professor/grade                → C1 enter/update grade
//   POST /api/grades/professor/courses/{id}/publish → C2 publish all grades
//   POST /api/grades/professor/grade/{id}/unpublish → C3 unpublish one grade
//   GET  /api/grades/ta/section                     → Q5 TA section tab
//   GET  /api/grades/ta/section/{id}                → Q6 TA section grade list
//   PUT  /api/grades/ta/attendance                  → C4 update attendance
//   PUT  /api/grades/ta/coursework                  → C5 enter quiz/discussion scores
//   POST /api/grades/ta/quizzes                     → C6 create quiz + seed QuizGrades
//   POST /api/grades/ta/discussions                 → C7 create discussion + seed DiscussionGrades

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
// These are what the client actually sends. CallerAppUserId is injected from JWT.

/// <summary>Body for PUT /api/grades/professor/grade</summary>
public record EnterGradeRequest(
    Guid     CourseGradeId,
    decimal? Midterm1Score,
    decimal? Midterm2Score,
    decimal? FinalExamScore,
    string?  ProfNote);

/// <summary>Body for PUT /api/grades/ta/attendance</summary>
public record UpdateAttendanceRequest(
    Guid    CourseGradeId,
    decimal AttendanceScore,
    bool    AttendanceOverridden);

/// <summary>Body for PUT /api/grades/ta/coursework</summary>
public record EnterCourseworkRequest(
    Guid                              CourseGradeId,
    IEnumerable<QuizScoreItem>        QuizScores,
    IEnumerable<DiscussionScoreItem>  DiscussionScores);

/// <summary>Body for POST /api/grades/ta/quizzes</summary>
public record CreateQuizRequest(
    Guid    SectionId,
    string  Title,
    decimal MaxScore);

/// <summary>Body for POST /api/grades/ta/discussions</summary>
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

    /// <summary>
    /// Q1 — Returns the list of semester tabs the student has grades in.
    /// </summary>
    [HttpGet("student/semesters")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStudentSemesterTabs()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new StudentSemesterTabQuery(callerId));
        return HandleResult(result);
    }

    /// <summary>
    /// Q2 — Returns all course grades for the student in one specific semester.
    /// </summary>
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

    /// <summary>
    /// Q3 — Returns the list of course offering tabs for the professor.
    /// </summary>
    [HttpGet("professor/courses")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> GetProfessorCourseTabs()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetProfessorCourseTabQuery(callerId));
        return HandleResult(result);
    }

    /// <summary>
    /// Q4 — Returns the full grade sheet for one course offering.
    /// </summary>
    [HttpGet("professor/courses/{courseOfferingId:guid}")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> GetProfessorCourseGrades(Guid courseOfferingId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetProfessorCourseGradesQuery(callerId, courseOfferingId));
        return HandleResult(result);
    }

    /// <summary>
    /// C1 — Professor enters or updates Midterm1, Midterm2, FinalExam for one student.
    /// Body: { courseGradeId, midterm1Score?, midterm2Score?, finalExamScore?, profNote? }
    /// CallerAppUserId is NOT in the body — injected from JWT.
    /// </summary>
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

    /// <summary>
    /// C2 — Professor publishes all ready grades for one course offering.
    /// </summary>
    [HttpPost("professor/courses/{courseOfferingId:guid}/publish")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> PublishGrades(Guid courseOfferingId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new PublishGradesCommand(callerId, courseOfferingId));
        return HandleResult(result);
    }

    /// <summary>
    /// C3 — Unpublishes one student's grade so the professor can correct it.
    /// </summary>
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

    /// <summary>
    /// Q5 — Returns the single section tab assigned to the TA.
    /// </summary>
    [HttpGet("ta/section")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> GetTaSectionTab()
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetTaSectionTabQuery(callerId));
        return HandleResult(result);
    }

    /// <summary>
    /// Q6 — Returns the full attendance/coursework grade list for the TA's section.
    /// </summary>
    [HttpGet("ta/section/{sectionId:guid}")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> GetTaSectionGrades(Guid sectionId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(new GetTaSectionGradesQuery(callerId, sectionId));
        return HandleResult(result);
    }

    /// <summary>
    /// C4 — TA updates the attendance score for one student.
    /// Body: { courseGradeId, attendanceScore, attendanceOverridden }
    /// CallerAppUserId is NOT in the body — injected from JWT.
    /// </summary>
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

    /// <summary>
    /// C5 — TA enters quiz and/or discussion scores for one student.
    /// Body: { courseGradeId, quizScores: [...], discussionScores: [...] }
    /// Use the QuizGradeIds and DiscussionGradeIds returned from C6/C7.
    /// CallerAppUserId is NOT in the body — injected from JWT.
    /// </summary>
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

    /// <summary>
    /// C6 — TA creates a new Quiz for their section.
    /// Seeds one QuizGrade (Score=0) per enrolled student automatically.
    /// Response includes QuizId + list of { StudentName, CourseGradeId, QuizGradeId }
    /// — use these QuizGradeIds in C5 to enter actual scores.
    /// Body: { sectionId, title, maxScore }
    /// </summary>
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
        return HandleResult(result);
    }

    /// <summary>
    /// C7 — TA creates a new Discussion for their section.
    /// Seeds one DiscussionGrade (Score=0) per enrolled student automatically.
    /// Response includes DiscussionId + list of { StudentName, CourseGradeId, DiscussionGradeId }
    /// — use these DiscussionGradeIds in C5 to enter actual scores.
    /// Body: { sectionId, title, maxScore }
    /// </summary>
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
        var result = await _sender.Send(command);
        return HandleResult(result);
    }
}
