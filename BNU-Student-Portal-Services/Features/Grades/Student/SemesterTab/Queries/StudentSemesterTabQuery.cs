using BNU_Student_Portal_Shared_Library.DTO_s.Semster;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.SemesterTab.Queries;

// as it can be called by TA, Professor and student
//
// FLOW DIAGRAM:
// [GET /api/grades/student/semesters]
//    -> [StudentSemesterTabQuery]
//    -> [StudentSemesterTabQueryHandler]
//         | resolve Student by AppUserId
//         | join Enrollment -> Section -> Offering -> Semester
//         | return unique SemesterTabDto list (newest first)
public record StudentSemesterTabQuery(string CallerId) : IRequest<Result<IEnumerable<SemesterTabDto>>>;
