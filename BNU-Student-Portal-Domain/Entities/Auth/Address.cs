using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Domain.Entities.Auth
{
    public class Address
    {
        public int Id { get; set; } //pk

        public string City { get; set; } = default!;

        public string Street { get; set; } = default!;

        public string Country { get; set; } = default!;

        public string Apartment { get; set; } 



        //navigational properties
        public AppUser User { get; set; } = default!;
        public string UserId { get; set; } = default!;

        public Guid GuardianId { get; set; }
        public Guardian Guardian { get; set; } = default!;
    }
}
