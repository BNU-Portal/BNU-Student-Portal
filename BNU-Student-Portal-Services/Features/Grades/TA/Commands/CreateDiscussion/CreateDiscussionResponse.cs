// FILE: Features/Grades/TA/Commands/CreateDiscussion/CreateDiscussionResponse.cs
// PURPOSE: Response DTO returned from C7 CreateDiscussion.
//          Contains the new DiscussionId + one entry per student so the TA
//          knows which DiscussionGradeId to pass into C5 EnterCoursework.

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.CreateDiscussion;

public record CreateDiscussionResponse(
    Guid    DiscussionId,
    string  Title,
    decimal MaxScore,
    IEnumerable<DiscussionStudentEntry> Students);

public record DiscussionStudentEntry(
    Guid CourseGradeId,
    Guid DiscussionGradeId);
