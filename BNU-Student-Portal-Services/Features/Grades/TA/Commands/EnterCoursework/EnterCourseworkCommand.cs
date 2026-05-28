// ============================================================
// FILE: Features/Grades/TA/Commands/EnterCoursework/EnterCourseworkCommand.cs
// LAYER: Application — CQRS Command (write-side)
// ============================================================
//
// PURPOSE:
//   A Teaching Assistant enters or updates quiz and discussion scores for
//   one student in their assigned section.
//
//   Quizzes and discussions each have their own child grade rows (QuizGrade,
//   DiscussionGrade) that are created/seeded when the section is set up.
//   The TA UPDATES those existing rows by ID — not creates new ones.
//
// SCORE CAPS (enforced by FluentValidation in the controller layer):
//   Quiz Total    → max 10 pts  (split across multiple quizzes)
//   Discussion    → max 15 pts  (split across multiple discussions)
//
// WHO CAN CALL THIS:
//   Only a user with the "TeachingAssistant" role.
//
// FLOW DIAGRAM:
//
//   HTTP PUT /api/grades/ta/coursework
//         │
//         │  Body: {
//         │    CourseGradeId,
//         │    QuizScores:       [ { QuizGradeId, Score }, ... ],
//         │    DiscussionScores: [ { DiscussionGradeId, Score }, ... ]
//         │  }
//         ▼
//   [GradesController.EnterCoursework]
//         │  extracts CallerAppUserId from JWT
//         ▼
//   MediatR.Send(EnterCourseworkCommand)
//         │
//         ▼
//   EnterCourseworkCommandHandler.Handle()
//         │  1. Resolve TA
//         │  2. Load parent CourseGrade
//         │  3. Verify section ownership
//         │  4. Block if IsPublished
//         │  5. For each QuizScoreItem → update QuizGrade
//         │  6. For each DiscussionScoreItem → update DiscussionGrade
//         │  7. SaveChangesAsync() — one transaction for all items
//         ▼
//   Result.Ok("Coursework scores updated successfully.")
//
// DESIGN NOTE:
//   Unknown IDs (QuizGradeId or DiscussionGradeId not matching this CourseGrade)
//   are silently skipped. This protects against frontend sending stale IDs
//   without crashing the whole request.
//
// CHILD RECORD STRUCTURES:
//
//   CourseGrade (1)
//       ├── QuizGrade (many)       — one row per quiz in the section
//       │     ├── Id (Guid)
//       │     ├── CourseGradeId    — FK back to parent
//       │     └── Score (decimal)
//       └── DiscussionGrade (many) — one row per discussion in the section
//             ├── Id (Guid)
//             ├── CourseGradeId    — FK back to parent
//             └── Score (decimal)
// ============================================================

using BNU_Student_Portal_Shared_Library.SharedResponse;
using MediatR;

namespace BNU_Student_Portal_Services.Features.Grades.TA.Commands.EnterCoursework;

// One item per quiz in the section
public record QuizScoreItem(Guid QuizGradeId, decimal Score);

// One item per discussion in the section
public record DiscussionScoreItem(Guid DiscussionGradeId, decimal Score);

public record EnterCourseworkCommand(
    string  CallerAppUserId,
    Guid    CourseGradeId,                           // the parent grade record
    IEnumerable<QuizScoreItem>       QuizScores,     // can be empty if no quizzes
    IEnumerable<DiscussionScoreItem> DiscussionScores) // can be empty if no discussions
    : IRequest<Result>;
