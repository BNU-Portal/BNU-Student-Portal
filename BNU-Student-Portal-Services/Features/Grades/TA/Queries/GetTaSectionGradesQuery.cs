// FILE: Features/Grades/TA/Queries/GetTaSectionGradesQuery.cs
// PURPOSE: TA clicks their section tab → returns the full attendance/coursework
//          grade list for all students in that section.
//          The TA can only view/edit attendance, quiz scores, and discussion scores —
//          NOT midterms or final exam (those are Professor-only).

using BNU_Student_Portal_Shared_Library.DTO_s.Grades;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Queries;

public record GetTaSectionGradesQuery(
    string CallerAppUserId,
    Guid   SectionId)       // the section the TA clicked on
    : IRequest<Result<TaSectionGradesDto>>;
