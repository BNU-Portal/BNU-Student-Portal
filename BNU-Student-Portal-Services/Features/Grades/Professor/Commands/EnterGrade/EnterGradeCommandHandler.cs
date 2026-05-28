// FILE: Features/Grades/Professor/Commands/EnterGrade/EnterGradeCommandHandler.cs
// PURPOSE: Validate ownership (grade belongs to professor's course), check publish
//          state, recompute AcademicWarning using GradeCalculator, then persist.
// NOTE:    CourseGrade is a class (not a record) — use direct property mutation,
//          NOT the 'with' expression.
//
// FLOW SUMMARY:
// Resolve professor -> find grade -> verify ownership -> block if published
// -> compute total -> set warning -> update and save
//
// DETAILED FLOW DIAGRAM:
//
//   [Professor Request] --> (JWT Validation: CallerAppUserId)
//            |
//            v
//   +----------------------+
//   | Fetch Professor      | <-- SELECT * FROM Professors WHERE AppUserId = @Id
//   +----------------------+
//            |
//            v
//   +----------------------+
//   | Fetch CourseGrade    | <-- SELECT * FROM CourseGrades WHERE Id = @GradeId
//   +----------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Ownership Validation | <--- | Enrollment -> Section -> Offering       |
//   | (Is this mine?)      |      | Check: Offering.ProfessorId == Prof.Id  |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+
//   | Publish Check        | --- [IsPublished?] --- (YES) --> [400 BadRequest]
//   +----------------------+          |
//            |                      (NO)
//            v                        |
//   +----------------------+          v
//   | Aggregate Components | <--- Fetch QuizGrades & DiscussionGrades
//   | (Sum Scores)         |      (Sum them for this CourseGrade)
//   +----------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | GradeCalculator      | <--- | Injects: Mid1, Mid2, Quiz, Disc,        |
//   | .Calculate()         |      | Attendance, Final                       |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+      +-----------------------------------------+
//   | Academic Warning     | <--- | IF FinalExam EXISTS AND Total < 60      |
//   | Logic                |      | THEN HasAcademicWarning = true          |
//   +----------------------+      +-----------------------------------------+
//            |
//            v
//   +----------------------+
//   | SQL Database Update  | <-- UPDATE CourseGrades SET ... WHERE Id = @Id
//   | (Unit of Work)       |
//   +----------------------+
//            |
//            v
//   [200 OK: Grade Updated]

using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.Professor.Commands.EnterGrade;

public class EnterGradeCommandHandler(IUnitOfWork _uow)
    : IRequestHandler<EnterGradeCommand, Result>
{
    public async Task<Result> Handle(EnterGradeCommand request, CancellationToken ct)
    {
        // ── Step 1: Resolve professor from JWT AppUserId ──────────────────────────
        var professors = await _uow.GetRepository<BNU_Student_Portal_Domain.Entities.Auth.Professor, Guid>().GetAllAsync();
        var professor  = professors.FirstOrDefault(p => p.AppUserId == request.CallerAppUserId);
        if (professor is null)
            return Result<object>.Fail(Error.NotFound("Grades.ProfessorNotFound", "Professor not found."));

        // ── Step 2: Load the grade record to update ───────────────────────────────
        var allGrades = await _uow.GetRepository<CourseGrade, Guid>().GetAllAsync();
        var grade     = allGrades.FirstOrDefault(g => g.Id == request.CourseGradeId);
        if (grade is null)
            return Result<object>.Fail(Error.NotFound("Grades.GradeNotFound", "Grade record not found."));

        // ── Step 3: Verify ownership — walk grade → enrollment → section → offering
        // This prevents a professor from editing another professor's student grades.
        var enrollments = await _uow.GetRepository<StudentSectionEnrollment, Guid>().GetAllAsync();
        var sections    = await _uow.GetRepository<CourseSection, Guid>().GetAllAsync();
        var offerings   = await _uow.GetRepository<CourseOffering, Guid>().GetAllAsync();

        var enrollment = enrollments.FirstOrDefault(e => e.Id == grade.EnrollmentId);
        var section    = sections.FirstOrDefault(s => s.Id == enrollment?.CourseSectionId);
        var offering   = offerings.FirstOrDefault(o => o.Id == section?.CourseOfferingId);

        if (offering?.ProfessorId != professor.Id)
            return Result<object>.Fail(Error.Forbidden("Grades.Forbidden",
                "This grade does not belong to your course."));

        // ── Step 4: Block edits on published grades — must unpublish first ─────────
        if (grade.IsPublished)
            return Result<object>.Fail(Error.BadRequest("Grades.AlreadyPublished",
                "Unpublish this grade before making changes."));

        // ── Step 5: Load quiz/discussion totals to recompute AcademicWarning ──────
        // AttendanceScore is TA-managed — read current value from the grade record.
        var quizGrades = await _uow.GetRepository<QuizGrade, Guid>().GetAllAsync();
        var discGrades = await _uow.GetRepository<DiscussionGrade, Guid>().GetAllAsync();

        var quizTotal = quizGrades.Where(q => q.CourseGradeId == grade.Id).Sum(q => q.Score);
        var discTotal = discGrades.Where(d => d.CourseGradeId == grade.Id).Sum(d => d.Score);

        // ── Step 6: Compute new total to determine if academic warning applies ────
        var (_, newTotal) = GradeCalculator.Calculate(
            request.Midterm1Score, request.Midterm2Score,
            discTotal, grade.AttendanceScore, quizTotal,
            request.FinalExamScore);

        // Warning only applies once a final exam score is present and total < 60
        var hasWarning = request.FinalExamScore.HasValue && newTotal < 60;

        // ── Step 7: Mutate and persist — CourseGrade is a class, not a record ─────
        grade.Midterm1Score      = request.Midterm1Score;
        grade.Midterm2Score      = request.Midterm2Score;
        grade.FinalExamScore     = request.FinalExamScore;
        grade.ProfNote           = request.ProfNote;
        grade.HasAcademicWarning = hasWarning;

        _uow.GetRepository<CourseGrade, Guid>().Update(grade);
        await _uow.SaveChangesAsync();

        return Result<object>.Ok("Grade updated successfully.");
    }
}
