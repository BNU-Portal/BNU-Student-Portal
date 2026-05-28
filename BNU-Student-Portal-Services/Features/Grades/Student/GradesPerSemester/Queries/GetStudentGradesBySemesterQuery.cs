using BNU_Student_Portal_Shared_Library.DTO_s.Semster;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.GradesPerSemester.Queries;

public record GetStudentGradesBySemesterQuery(
    string CallerAppUserId,
    Guid   SemesterId)           // Which semester tab was clicked
    : IRequest<Result<StudentSemesterSummaryDto>>;