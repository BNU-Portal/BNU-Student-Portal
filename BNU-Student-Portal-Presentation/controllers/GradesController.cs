// FILE: Presentation/controllers/GradesController.cs
// PURPOSE: Exposes all Grades endpoints for Student, Professor, and TA roles.
//
// SECURITY PATTERN: CallerAppUserId is ALWAYS extracted from the validated JWT
//   using User.FindFirstValue(ClaimTypes.NameIdentifier) inside each action.
//   It is never read from the request body — this prevents impersonation.
//   For commands sent as JSON body (EnterGrade, UpdateAttendance, EnterCoursework),
//   the CallerAppUserId in the body is overwritten with the JWT value via 'with'.
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

// END-TO-END FLOW (one student, one course, one offering):
// Admin:
//   CreateSemester -> ActivateSemester -> CreateCourseOffering -> CreateCourseSection -> EnrollStudent
// TA:
//   GetTaSectionTab -> GetTaSectionGrades -> UpdateAttendance / EnterCoursework
// Professor:
//   GetProfessorCourseTab -> GetProfessorCourseGrades -> EnterGrade -> PublishGrades
// Student:
//   GetStudentSemesterTabs -> GetStudentGradesBySemester (only after publish)
//
// MASTER DIAGRAM (identity + data flow + visibility gates):
//
//   [JWT NameIdentifier]
//        |
//        v
//   [Controller Action] --(sets CallerAppUserId)--> [MediatR Command/Query]
//        |                                             |
//        |                                             v
//        |                                       [Handler]
//        |                                             |
//        |             +-------------------------------+-----------------------------+
//        |             |                                                             |
//        v             v                                                             v
//   Student flow   TA flow                                                      Professor flow
//   -----------   --------                                                      --------------
//   Tabs/Grades   Attendance/Coursework                                        Midterms/Finals
//        |             |                                                             |
//        v             v                                                             v
//   [CourseGrade] <----+-------------------------- shared grade record ---------------+
//        |
//        |  Visibility Gate (student):
//        |  IsPublished == true AND FinalExamScore.HasValue
//        v
//   [Student view sees totals, letter, GPA]

using BNU_Student_Portal_Services.Features.Grades.Professor.Commands.EnterGrade;
using BNU_Student_Portal_Services.Features.Grades.Professor.Commands.PublishGrades;
using BNU_Student_Portal_Services.Features.Grades.Professor.Commands.UnpublishGrade;
using BNU_Student_Portal_Services.Features.Grades.Professor.Queries;
using BNU_Student_Portal_Services.Features.Grades.Student.GradesPerSemester.Queries;
using BNU_Student_Portal_Services.Features.Grades.Student.SemesterTab.Queries;
using BNU_Student_Portal_Services.Features.Grades.TA.Commands.EnterCoursework;
using BNU_Student_Portal_Services.Features.Grades.TA.Commands.UpdateAttendance;
using BNU_Student_Portal_Services.Features.Grades.TA.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BNU_Student_Portal_Presentation.Controllers;

[ApiController]
[Route("api/grades")]
[Authorize]  // all endpoints require a valid JWT — role checks are per-action
public class GradesController(ISender _sender) : ApiBaseController
{
    // ════════════════════════════════════════════════════════════════════
    // STUDENT ENDPOINTS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Q1 — Returns the list of semester tabs the student has grades in.
    /// Frontend uses this to render the top tab bar on the student grades page.
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
    /// Called when the student clicks a semester tab.
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
    /// Each tab shows one course with publish status and pending student count.
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
    /// Q4 — Returns the full grade sheet (all students + distribution) for one
    /// course offering. Called when the professor clicks a course tab.
    /// </summary>
    [HttpGet("professor/courses/{courseOfferingId:guid}")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> GetProfessorCourseGrades(Guid courseOfferingId)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result   = await _sender.Send(
            new GetProfessorCourseGradesQuery(callerId, courseOfferingId));
        return HandleResult(result);
    }

    /// <summary>
    /// C1 — Professor enters or updates Midterm1, Midterm2, FinalExam for one student.
    /// CallerAppUserId from the request body is overwritten with the JWT identity.
    /// </summary>
    [HttpPut("professor/grade")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> EnterGrade([FromBody] EnterGradeCommand command)
    {
        // Overwrite identity from JWT — never trust the client-supplied value
        var safeCommand = command with
        {
            CallerAppUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await _sender.Send(safeCommand);
        return HandleResult(result);
    }

    /// <summary>
    /// C2 — Professor publishes all ready grades for one course offering at once.
    /// Only grades with a FinalExamScore are published; already-published ones are skipped.
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
    /// After fixing the score, call PublishGrades again to re-publish.
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
    /// TAs are assigned to exactly one section per semester.
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
    /// The TA sees attendance, quiz totals, and discussion totals — NOT midterms or final.
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
    /// Set AttendanceOverridden = true to flag that this was a manual override.
    /// </summary>
    [HttpPut("ta/attendance")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> UpdateAttendance([FromBody] UpdateAttendanceCommand command)
    {
        var safeCommand = command with
        {
            CallerAppUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await _sender.Send(safeCommand);
        return HandleResult(result);
    }

    /// <summary>
    /// C5 — TA enters quiz and/or discussion scores for one student.
    /// Pass the specific QuizGradeId / DiscussionGradeId for each item to update.
    /// Unknown IDs are silently skipped.
    /// </summary>
    [HttpPut("ta/coursework")]
    [Authorize(Roles = "TeachingAssistant")]
    public async Task<IActionResult> EnterCoursework([FromBody] EnterCourseworkCommand command)
    {
        var safeCommand = command with
        {
            CallerAppUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await _sender.Send(safeCommand);
        return HandleResult(result);
    }
}
