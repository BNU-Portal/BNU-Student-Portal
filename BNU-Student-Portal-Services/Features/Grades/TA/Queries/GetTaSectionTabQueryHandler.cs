// FILE: Features/Grades/TA/Queries/GetTaSectionTabQueryHandler.cs
// PURPOSE: Resolve the TA from their AppUserId, find their assigned section,
//          and return a lightweight tab DTO with course/semester context.

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
        // A TA is assigned to exactly one section — return 404 if none assigned yet.
        var section = sections.FirstOrDefault(s => s.TeachingAssistantId == ta.Id);
        if (section is null)
            return Result<TaSectionTabDto>.Fail(
                Error.NotFound("Grades.NoSection", "No section is currently assigned to you."));

        // ── Step 4: Walk section → offering → course → semester for display data ─
        var offering = offerings.FirstOrDefault(o => o.Id == section.CourseOfferingId);
        var course   = courses.FirstOrDefault(c => c.Id == offering?.CourseId);
        var semester = semesters.FirstOrDefault(s => s.Id == offering?.SemesterId);

        // ── Step 5: Count students currently enrolled in this section ─────────────
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
