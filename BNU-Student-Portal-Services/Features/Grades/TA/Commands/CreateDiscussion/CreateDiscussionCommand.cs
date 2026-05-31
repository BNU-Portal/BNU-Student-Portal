// FILE: Features/Grades/TA/Commands/CreateDiscussion/CreateDiscussionCommand.cs
// PURPOSE: TA creates a new Discussion activity for their section.
//          After creation, a DiscussionGrade row (Score=0) is seeded for every
//          enrolled student so EnterCoursework has valid DiscussionGradeIds to update.
//
// NOTE: CallerAppUserId is string? (nullable) — injected from JWT in controller.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateDiscussion;

public record CreateDiscussionCommand(
    string? CallerAppUserId,   // injected from JWT in controller — never from body
    Guid    SectionId,
    string  Title,
    decimal MaxScore)
    : IRequest<Result<CreateDiscussionResponse>>;
