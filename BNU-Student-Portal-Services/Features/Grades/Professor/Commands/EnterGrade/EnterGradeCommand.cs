// FILE: Features/Grades/Professor/Commands/EnterGrade/EnterGradeCommand.cs
// PURPOSE: Professor enters or updates Midterm1, Midterm2, FinalExam scores
//          and an optional note for a single student's CourseGrade record.
//          AcademicWarning is auto-computed in the handler — not set by the caller.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Commands.EnterGrade;

public record EnterGradeCommand(
    string   CallerAppUserId,   // overwritten from JWT in the controller — never trust body
    Guid     CourseGradeId,     // the specific grade record to update
    decimal? Midterm1Score,     // 0–15, nullable (not entered yet is valid)
    decimal? Midterm2Score,     // 0–15, nullable
    decimal? FinalExamScore,    // 0–40, nullable
    string?  ProfNote)          // optional internal note visible only to professor
    : IRequest<Result>;
