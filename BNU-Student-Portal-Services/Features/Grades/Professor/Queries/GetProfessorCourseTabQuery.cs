using BNU_Student_Portal_Shared_Library.DTO_s.Courses;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Queries;

// FLOW DIAGRAM:
// [GET /api/grades/professor/courses]
//    -> [GetProfessorCourseTabQuery]
//    -> [GetProfessorCourseTabQueryHandler]
//    -> returns ProfessorCourseTabDto list
public record GetProfessorCourseTabQuery(
    string CallerAppUserId)   // from JWT claim
    : IRequest<Result<IEnumerable<ProfessorCourseTabDto>>>;
