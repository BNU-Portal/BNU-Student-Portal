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
//    -> returns SemesterTabDto list
public record StudentSemesterTabQuery(string CallerId) : IRequest<Result<IEnumerable<SemesterTabDto>>>;
