// FILE: Features/Student/Enrollment/Commands/SelfEnroll/SelfEnrollCommand.cs
// PURPOSE: Student self-enrolls into a section.
//          StudentAppUserId is extracted from the JWT token in the controller — never sent in body.
//          Enforces MaxStudents cap — rejects if section is full.
//          Auto-creates a blank CourseGrade row alongside the enrollment.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Student.Enrollment.Commands.SelfEnroll;

public record SelfEnrollCommand(
    string StudentAppUserId,   // extracted from JWT in controller
    Guid   CourseSectionId)
    : IRequest<Result<SelfEnrollResult>>;

public record SelfEnrollResult(Guid EnrollmentId, Guid CourseGradeId);
