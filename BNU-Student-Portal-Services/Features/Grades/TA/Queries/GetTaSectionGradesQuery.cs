// ============================================================
// FILE: Features/Grades/TA/Queries/GetTaSectionGradesQuery.cs
// LAYER: Application — CQRS Query (read-side)
// ============================================================
//
// PURPOSE:
//   When a TA clicks on their section tab, this query loads the full
//   student grade list for that section — showing attendance, quiz totals,
//   and discussion totals for every enrolled student.
//
// WHAT THE TA CAN SEE (read) vs. WHAT ONLY THE PROFESSOR SEES:
//
//   TA CAN SEE:                     PROFESSOR-ONLY:
//   ─────────────────────────────   ──────────────────────
//   AttendanceScore                 Midterm1Score
//   AttendanceOverridden            Midterm2Score
//   QuizTotal (summed)              FinalExamScore
//   DiscussionTotal (summed)        IsPublished toggle
//   HasAcademicWarning              ProfNote (write)
//   ProfNote (read-only)            Full grade distribution
//   StudentName                     StudentNationalId
//
// SECURITY:
//   The handler verifies that the requested SectionId has
//       CourseSection.TeachingAssistantId == ta.Id
//   before returning any data. A TA cannot query another TA's section.
//
// FLOW DIAGRAM:
//
//   TA clicks section tab
//         │
//         ▼
//   GET /api/grades/ta/section/{sectionId}
//         │  JWT extracts CallerAppUserId
//         ▼
//   MediatR.Send(GetTaSectionGradesQuery(CallerAppUserId, SectionId))
//         │
//         ▼
//   GetTaSectionGradesQueryHandler
//         │  Returns TaSectionGradesDto with list of TaGradeRowDto
//         ▼
//   { SectionId, SectionName, CourseCode, CourseName, SemesterName,
//     StudentCount, Students: [ TaGradeRowDto... ] }
// ============================================================

using BNU_Student_Portal_Shared_Library.DTO_s.Grades;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Queries;

public record GetTaSectionGradesQuery(
    string CallerAppUserId,
    Guid   SectionId)       // the section the TA clicked on
    : IRequest<Result<TaSectionGradesDto>>;
