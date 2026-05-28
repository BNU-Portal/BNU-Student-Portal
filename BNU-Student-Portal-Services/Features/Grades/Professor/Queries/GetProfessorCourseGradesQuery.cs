using BNU_Student_Portal_Shared_Library.DTO_s.Grades;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Queries;

// FLOW DIAGRAM:
// [GET /api/grades/professor/courses/{id}]
//    -> [GetProfessorCourseGradesQuery]
//    -> [GetProfessorCourseGradesQueryHandler]
//    -> returns ProfessorCourseGradesDto
public record GetProfessorCourseGradesQuery(
    string CallerAppUserId,
    Guid   CourseOfferingId)   // which tab was clicked
    : IRequest<Result<ProfessorCourseGradesDto>>;
