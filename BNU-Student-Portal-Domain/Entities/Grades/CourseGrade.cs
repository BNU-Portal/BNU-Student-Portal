using BNU_Student_Portal_Domain.Entities.Sections;

namespace BNU_Student_Portal_Domain.Entities.Grades;

 // The central grade record for one student in one section.
    // Every score component lives here (midterms, attendance, final)
    // or hangs off this entity (quiz grades, discussion grades).
    //
    // SCORE STRUCTURE (mirrors BNU business rules from Section 1):
    //   Midterm1Score  max 15   (Professor only)
    //   Midterm2Score  max 15   (Professor only)
    //   AttendanceScore max 5   (auto from QR, overridable)
    //   QuizGrades     variable (sum is part of coursework)
    //   DiscussionGrades variable
    //   ─── All of the above are normalized to /100 → CourseWork ───
    //   FinalExamScore  max 100  (Professor only)
    //   Total = CourseWork + FinalExam → /200
    //
    // IsPublished controls student visibility.
    // HasAcademicWarning is a flag toggled by Prof/TA.
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