using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Entities.Courses;
using BNU_Student_Portal_Domain.Entities.Discussions;
using BNU_Student_Portal_Domain.Entities.Grades;
using BNU_Student_Portal_Domain.Entities.Quizzes;
using BNU_Student_Portal_Domain.Entities.Sections;
using BNU_Student_Portal_Domain.Entities.Semesters;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BNU_Student_Portal_Persistence.Data.DbContext
{
    public class BNU_Student_Portal_DbContext(DbContextOptions<BNU_Student_Portal_DbContext> options)
        : IdentityDbContext<AppUser>(options)
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Professor> Professors { get; set; }
        public DbSet<TeachingAssistant> TeachingAssistants { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<Address> Addresses { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        //Grades and courses 
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseOffering> CourseOfferings { get; set; }
        public DbSet<CourseSection> CourseSections { get; set; }
        public DbSet<StudentSectionEnrollment> StudentSectionEnrollments { get; set; }
        public DbSet<CourseGrade> CourseGrades { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizGrade> QuizGrades { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<DiscussionGrade> DiscussionGrades { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional model configurations can be added here
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BNU_Student_Portal_DbContext).Assembly);

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            #region Course offering and section

            //Course offering and section
            modelBuilder.Entity<CourseOffering>(entity =>
            {
                // CourseOffering → Course (many offerings can exist for one course,
                // e.g. "Machine Learning" offered in Term 1 AND Term 2)
                // Restrict: you cannot delete a Course that has offerings attached to it.
                entity.HasOne(o => o.Course)
                    .WithMany(c => c.Offerings)
                    .HasForeignKey(o => o.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                // CourseOffering → Semester (many offerings exist per semester,
                // e.g. Semester "Term 2 2025-2026" has many course offerings)
                // Restrict: you cannot delete a Semester that has active offerings.
                entity.HasOne(o => o.Semester)
                    .WithMany(s => s.CourseOfferings)
                    .HasForeignKey(o => o.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                // CourseOffering → Professor (one professor teaches this LEC)
                // WithMany() empty — Professor entity has no navigation back to offerings.
                // Restrict: you cannot delete a Professor who is assigned to a course.
                entity.HasOne(o => o.Professor)
                    .WithMany()
                    .HasForeignKey(o => o.ProfessorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region Sections

            //Sections 
            modelBuilder.Entity<CourseSection>(entity =>
            {
                // CourseSection → CourseOffering (many SECs belong to one LEC,
                // e.g. "Machine Learning Term2" has Section 1, Section 2, Section 3)
                // Restrict: you cannot delete a CourseOffering that still has sections.
                // We do NOT use Cascade here because deleting a LEC should be a
                // conscious decision — not silently wipe all SECs and student grades.
                entity.HasOne(cs => cs.CourseOffering)
                    .WithMany(o => o.Sections)
                    .HasForeignKey(cs => cs.CourseOfferingId)
                    .OnDelete(DeleteBehavior.Restrict);

                // CourseSection → TeachingAssistant (one TA runs this SEC)
                // WithMany() empty — TA entity has no navigation back to sections.
                // Restrict: you cannot delete a TA who is assigned to a section.
                entity.HasOne(cs => cs.TeachingAssistant)
                    .WithMany()
                    .HasForeignKey(cs => cs.TeachingAssistantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion


            #region Enrollments

            modelBuilder.Entity<StudentSectionEnrollment>(entity =>
            {
                // Enrollment → Student
                // Restrict: you cannot delete a Student who has enrollments.
                // Keeps historical grade data safe even if a student leaves.
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Enrollment → CourseSection
                // Cascade: if a SEC is deleted, all its enrollments are deleted too.
                // This makes sense — the SEC is the parent container here.
                // NOTE: CourseGrade will also cascade from Enrollment (see below),
                // so deleting a SEC triggers: SEC → Enrollments → CourseGrades chain.
                entity.HasOne(e => e.CourseSection)
                    .WithMany(cs => cs.Enrollments)
                    .HasForeignKey(e => e.CourseSectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            #endregion

            #region Grades

            modelBuilder.Entity<CourseGrade>(entity =>
            {
                // CourseGrade → StudentSectionEnrollment (ONE-TO-ONE)
                // One enrollment has exactly one grade record.
                // HasForeignKey<CourseGrade> — EF needs explicit direction here
                // because it is a one-to-one, not one-to-many.
                // Cascade: if an enrollment is deleted, its grade record is deleted too.
                entity.HasOne(g => g.Enrollment)
                    .WithOne(e => e.CourseGrade)
                    .HasForeignKey<CourseGrade>(g => g.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            #endregion

            #region Quizzes Grades

            modelBuilder.Entity<QuizGrade>(entity =>
            {
                // QuizGrade → CourseGrade (many quiz grades belong to one course grade)
                // Cascade: if a CourseGrade is deleted, all its QuizGrades are deleted.
                // This is correct — a quiz grade has no meaning without its parent grade record.
                entity.HasOne(qg => qg.CourseGrade)
                    .WithMany(g => g.QuizGrades)
                    .HasForeignKey(qg => qg.CourseGradeId)
                    .OnDelete(DeleteBehavior.Cascade);

                // QuizGrade → Quiz (many grades point to one quiz definition)
                // Restrict: you cannot delete a Quiz that already has grades recorded.
                // This protects data integrity — grades should be cleared before
                // removing a quiz, not silently wiped.
                entity.HasOne(qg => qg.Quiz)
                    .WithMany(q => q.Grades)
                    .HasForeignKey(qg => qg.QuizId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            #region Discussions Grades

            modelBuilder.Entity<DiscussionGrade>(entity =>
            {
                // DiscussionGrade → CourseGrade
                // Cascade: same reasoning as QuizGrade — no meaning without parent.
                entity.HasOne(dg => dg.CourseGrade)
                    .WithMany(g => g.DiscussionGrades)
                    .HasForeignKey(dg => dg.CourseGradeId)
                    .OnDelete(DeleteBehavior.Cascade);

                // DiscussionGrade → Discussion
                // Restrict: same reasoning as Quiz — protect recorded grades.
                entity.HasOne(dg => dg.Discussion)
                    .WithMany(d => d.Grades)
                    .HasForeignKey(dg => dg.DiscussionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion
        }
    }
}