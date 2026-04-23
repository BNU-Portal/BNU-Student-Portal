using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Domain.Entities.Auth
{
    public class Guardian : BaseEntity<Guid>
    {
        public AppUser User { get; set; } = default!;
        public string UserId { get; set; } = default!;


        public Student Student { get; set; }
        public Guid StudentId { get; set; }


        public string FathersJob { get; set; } = default!;
        public string MothersJob { get; set; } = default!;


        public string FatherName { get; set; } = default!;
        public string FatherPhone { get; set; } = default!;
        public string MotherName { get; set; } = default!;
        public string MotherPhone { get; set; } = default!;
        public bool IsFatherDeceased { get; set; }
        public bool IsMotherDeceased { get; set; }







    }
}
