// FILE: Features/Grades/Professor/Commands/UnpublishGrade/UnpublishGradeCommand.cs
// PURPOSE: Unpublish ONE student's grade so the professor can correct a mistake.
//          After unpublishing, the professor can call EnterGrade to fix scores,
//          then PublishGrades again to re-publish.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Commands.UnpublishGrade;

public record UnpublishGradeCommand(
    string CallerAppUserId,  // from JWT
    Guid   CourseGradeId)    // the specific grade record to unpublish
    : IRequest<Result>;
