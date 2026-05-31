// FILE: Features/Grades/Student/CourseDetail/Queries/GetCourseGradeDetailQuery.cs
// PURPOSE: Student clicks a course row → returns every quiz and discussion
//          individually (title, score, maxScore, note) plus exam scores and prof note.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Student.CourseDetail.Queries;

public record GetCourseGradeDetailQuery(
    string? CallerAppUserId,
    Guid    CourseGradeId)
    : IRequest<Result<CourseGradeDetailDto>>;
