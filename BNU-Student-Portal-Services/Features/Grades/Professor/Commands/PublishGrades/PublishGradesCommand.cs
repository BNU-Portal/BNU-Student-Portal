// FILE: Features/Grades/Professor/Commands/PublishGrades/PublishGradesCommand.cs
// PURPOSE: Professor publishes ALL ready grades for one CourseOffering at once.
//          "Ready" means the grade has a FinalExamScore and is not already published.
//          Once published, students can see their grades.
//
// FLOW DIAGRAM:
// [POST /api/grades/professor/courses/{id}/publish]
//    -> [PublishGradesCommand]
//    -> [PublishGradesCommandHandler]
//    -> marks matching CourseGrade rows IsPublished = true

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Commands.PublishGrades;

public record PublishGradesCommand(
    string CallerAppUserId,   // from JWT
    Guid   CourseOfferingId)  // which course offering to publish
    : IRequest<Result>;
