// FILE: Features/Admin/CourseOffering/Commands/CreateCourseOffering/CreateCourseOfferingCommand.cs
// PURPOSE: Admin links a Course + Semester + Professor into one CourseOffering.

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Admin.CourseOffering.Commands.CreateCourseOffering;

public record CreateCourseOfferingCommand(
    Guid CourseId,
    Guid SemesterId,
    Guid ProfessorId)
    : IRequest<Result<Guid>>;
