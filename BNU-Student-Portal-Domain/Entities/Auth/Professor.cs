using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Domain.Entities.Auth
{
    public class Professor : BaseEntity<Guid>
    {
        public string AppUserId { get; set; } = default!;
        public AppUser AppUser { get; set; } = default!;

        // Professor-specific fields
        public string Department { get; set; } = default!;
        public string OfficeLocation { get; set; } = default!;
    }
}
