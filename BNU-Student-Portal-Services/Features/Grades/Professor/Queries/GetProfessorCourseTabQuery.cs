using BNU_Student_Portal_Shared_Library.DTO_s.Courses;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Queries;

public record GetProfessorCourseTabQuery(
    string CallerAppUserId)   // from JWT claim
    : IRequest<Result<IEnumerable<ProfessorCourseTabDto>>>;