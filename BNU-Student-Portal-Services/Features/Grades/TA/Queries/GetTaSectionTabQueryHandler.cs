// ============================================================
// FILE: Features/Grades/TA/Queries/GetTaSectionTabQueryHandler.cs
// LAYER: Application — CQRS Query Handler
// ============================================================
//
// PURPOSE:
//   Resolves the TA's identity, finds their single assigned section,
//   and returns a lightweight DTO for the section tab in the UI.
//
// TABLE CHAIN (all loaded in-memory, no EF navigation properties):
//
//   JWT AppUserId
//       │
//       ▼
//   TeachingAssistant (ta.Id)
//       │
//       ▼  CourseSection.TeachingAssistantId == ta.Id
//   CourseSection
//       │  section.CourseOfferingId
//       ▼
//   CourseOffering
//       │  offering.CourseId   /  offering.SemesterId
//       ▼                           ▼
//    Course                      Semester
//    (code, name)                (name)
//
//   StudentSectionEnrollment (count where CourseSectionId == section.Id)
//       → StudentCount for the tab badge
//
// DETAILED FLOW DIAGRAM:
//
//   [Request: TA JWT] --> (AppUserId Resolution)
//            |
//            v
//   +----------------------+
//   | Fetch TA             | <-- Match JWT Subject to TeachingAssistant Table
//   +----------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Find Section         | <--- | SELECT * FROM Sections                  |
//   | (Assignment Check)   |      | WHERE TeachingAssistantId = @TAId       |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Fetch Context        | <--- | Section -> Offering -> Course & Semester|
//   | (Joins for Display)  |      | (Get names and codes for the UI)        |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Count Enrollments    | <--- | SELECT COUNT(*) FROM Enrollments        |
//   | (Badge Data)         |      | WHERE CourseSectionId = @SectionId      |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+
//   | Return Tab Info      | --- TaSectionTabDto
//   +----------------------+
//            |
//            v
//   [200 OK: Section Dashboard]
// ============================================================

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.DTO_s.Sections;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Queries;

public class GetTaSectionTabQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetTaSectionTabQuery, Result<TaSectionTabDto>>
{
    public async Task<Result<TaSectionTabDto>> Handle(
        GetTaSectionTabQuery request, CancellationToken ct)
    {
        // ── Step 1: Resolve the TeachingAssistant row from the JWT AppUserId ─────
        // AppUserId is the IdentityUser.Id string from the JWT claim.
        // We need the domain TeachingAssistant entity to get ta.Id (Guid)
        // for matching against CourseSection.TeachingAssistantId.
        var tas = await _uow.GetRepository<TeachingAssistant, Guid>().GetAllAsync();
        var ta  = tas.FirstOrDefault(t => t.AppUserId == request.CallerAppUserId);
        if (ta is null)
            return Result<TaSectionTabDto>.Fail(
                Error.NotFound("Grades.TaNotFound", "Teaching Assistant profile not found."));

        // ── Step 2: Load all tables needed for the response ───────────────────────
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings   = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();
        var courses     = await _uow.GetRepository<Course, Guid>().GetAllAsync();
        var semesters   = await _uow.GetRepository<Semester, Guid>().GetAllAsync();
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();

        // ── Step 3: Find the section where TeachingAssistantId == ta.Id ──────────
        // A TA is assigned to exactly one section by the Admin.
        // If none found, they haven't been assigned yet — return a clear 404.
        var section = sections.FirstOrDefault(s => s.TeachingAssistantId == ta.Id);
        if (section is null)
            return Result<TaSectionTabDto>.Fail(
                Error.NotFound("Grades.NoSection", "No section is currently assigned to you."));

        // ── Step 4: Walk section → offering → course → semester for display data ─
        // All lookups are null-safe. If any link in the chain is broken
        // (orphaned FK), the DTO gets empty strings rather than throwing.
        var offering = offerings.FirstOrDefault(o => o.Id == section.CourseOfferingId);
        var course   = courses.FirstOrDefault(c => c.Id == offering?.CourseId);
        var semester = semesters.FirstOrDefault(s => s.Id == offering?.SemesterId);

        // ── Step 5: Count students currently enrolled in this section ─────────────
        // Used by the frontend to show the student count badge on the tab.
        var studentCount = enrollments.Count(e => e.CourseSectionId == section.Id);

        return Result<TaSectionTabDto>.Ok(new TaSectionTabDto
        {
            SectionId    = section.Id,
            SectionName  = section.SectionName,
            CourseCode   = course?.Code   ?? string.Empty,
            CourseName   = course?.Name   ?? string.Empty,
            SemesterName = semester?.Name ?? string.Empty,
            StudentCount = studentCount
        });
    }
}
