// FILE: Features/Grades/TA/Queries/GetTaSectionTabQuery.cs
// PURPOSE: TA opens the Grades page → returns the single section assigned to them
//          so the frontend can render the section tab header.
//          A TA is assigned to exactly one CourseSection via CourseSection.TeachingAssistantId.

using BNU_Student_Portal_Shared_Library.DTO_s.Sections;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Queries;

// CallerAppUserId comes from the JWT NameIdentifier claim — injected in the controller.
public record GetTaSectionTabQuery(
    string CallerAppUserId)
    : IRequest<Result<TaSectionTabDto>>;
