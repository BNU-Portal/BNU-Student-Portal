using BNU_Student_Portal_Shared_Library.DTO_s.Semster;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.SemesterTab.Queries;

//as it can be called by TA, Professor and student
public record StudentSemesterTabQuery(string CallerId) : IRequest<Result<IEnumerable<SemesterTabDto>>>;