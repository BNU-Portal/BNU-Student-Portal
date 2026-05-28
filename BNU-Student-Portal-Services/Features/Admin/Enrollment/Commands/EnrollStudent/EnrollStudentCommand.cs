// FILE: Features/Admin/Enrollment/Commands/EnrollStudent/EnrollStudentCommand.cs
// PURPOSE: Admin enrolls a student into a section.
//          A blank CourseGrade row is automatically created alongside the enrollment.
//
// FLOW DIAGRAM:
// [POST /api/admin/enrollments]
//    -> [EnrollStudentCommand(StudentId, CourseSectionId)]
//    -> [EnrollStudentCommandHandler]
//         | validate student + section
//         | prevent duplicate enrollment
//         | create Enrollment + CourseGrade
//    -> returns EnrollmentId + CourseGradeId

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Enrollment.Commands.EnrollStudent;

public record EnrollStudentCommand(
    Guid StudentId,
    Guid CourseSectionId)
    : IRequest<Result<EnrollmentResult>>;

public record EnrollmentResult(Guid EnrollmentId, Guid CourseGradeId);
