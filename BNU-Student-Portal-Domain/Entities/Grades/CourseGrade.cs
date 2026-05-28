using BNU_Student_Portal_Domain.Entities.Sections;

namespace BNU_Student_Portal_Domain.Entities.Grades;

// SCORE STRUCTURE (BNU business rules):
//   Midterm1Score   max 15   ─┐
//   Midterm2Score   max 15   ─┤ Midterms total /30
//   AttendanceScore max 5    ─┤
//   QuizGrades      sum /10  ─┤ CourseWork total /60
//   DiscussionGrades sum /15 ─┘
//   FinalExamScore  max 40
//   Total = CourseWork(/60) + FinalExam(/40) = /100
//
//   IsPublished controls student visibility.
//   HasAcademicWarning is toggled by Prof/TA.
    public class CourseGrade : BaseEntity<Guid>
    {
        public Guid                      EnrollmentId { get; set; }
        public StudentSectionEnrollment  Enrollment   { get; set; } = default!;

        // Midterms — max 15 each, Professor only
        public decimal? Midterm1Score { get; set; }
        public string?  Midterm1Note  { get; set; }
        public decimal? Midterm2Score { get; set; }
        public string?  Midterm2Note  { get; set; }

        // Attendance — auto-filled by QR system, manually overridable
        public decimal AttendanceScore    { get; set; } = 0;
        public decimal AttendanceMaxScore { get; set; } = 5;
        public bool    AttendanceOverridden { get; set; } = false;

        // Final Exam — max 100, Professor only
        public decimal? FinalExamScore { get; set; }

        // Publishing and warning flags
        public bool    IsPublished        { get; set; } = false;
        public bool    HasAcademicWarning { get; set; } = false;
        public string? ProfNote           { get; set; }  // internal note

        public ICollection<QuizGrade>       QuizGrades       { get; set; } = new List<QuizGrade>();
        public ICollection<DiscussionGrade> DiscussionGrades { get; set; } = new List<DiscussionGrade>();
    }