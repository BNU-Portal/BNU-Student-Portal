// FILE: Features/Admin/Enrollment/Commands/EnrollStudent/EnrollStudentCommand.cs
// PURPOSE: Admin enrolls a student into a section.
//          A blank CourseGrade row is automatically created alongside the enrollment.
//
// NOTE: StudentAppUserId is the AppUser.Id (string FK), NOT the Student.Id (PK).
//       The handler resolves Student.Id from AppUserId internally.
//
// FLOW DIAGRAM:
// [POST /api/admin/enrollments]
//    -> [EnrollStudentCommand(StudentAppUserId, CourseSectionId)]
//    -> [EnrollStudentCommandHandler]
//         | resolve student by AppUserId
//         | validate section exists
//         | prevent duplicate enrollment
//         | create Enrollment + CourseGrade
//    -> returns EnrollmentId + CourseGradeId

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.Enrollment.Commands.EnrollStudent;

public record EnrollStudentCommand(
    string StudentAppUserId,
    Guid   CourseSectionId)
    : IRequest<Result<EnrollmentResult>>;

public record EnrollmentResult(Guid EnrollmentId, Guid CourseGradeId);
