// ============================================================
// FILE: Features/Grades/TA/Queries/GetTaSectionTabQuery.cs
// LAYER: Application — CQRS Query (read-side)
// ============================================================
//
// PURPOSE:
//   When a TA opens the Grades page, the frontend needs to know which section
//   tab to display. This query returns a lightweight TaSectionTabDto that
//   provides the section name, course info, semester, and student count —
//   everything the frontend needs to render the tab header.
//
// KEY DESIGN FACT:
//   A TA is assigned to exactly ONE CourseSection via:
//       CourseSection.TeachingAssistantId == ta.Id
//   This is a 1:1 relationship — a TA does not get to pick a section,
//   they are assigned one by the admin.
//
// FLOW:
//
//   TA opens Grades page
//         │
//         ▼
//   GET /api/grades/ta/section-tab
//         │  JWT extracts CallerAppUserId
//         ▼
//   MediatR.Send(GetTaSectionTabQuery(CallerAppUserId))
//         │
//         ▼
//   GetTaSectionTabQueryHandler
//         │  1. Resolve TA from AppUserId
//         │  2. Find section where TeachingAssistantId == ta.Id
//         │  3. Walk: section → offering → course → semester
//         │  4. Count enrollments in this section
//         │  5. Return TaSectionTabDto
//         ▼
//   { SectionId, SectionName, CourseCode, CourseName, SemesterName, StudentCount }
//
// CallerAppUserId comes from the JWT NameIdentifier claim — injected in the controller.
// ============================================================

using BNU_Student_Portal_Shared_Library.DTO_s.Sections;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Queries;

// CallerAppUserId comes from the JWT NameIdentifier claim — injected in the controller.
public record GetTaSectionTabQuery(
    string CallerAppUserId)
    : IRequest<Result<TaSectionTabDto>>;
