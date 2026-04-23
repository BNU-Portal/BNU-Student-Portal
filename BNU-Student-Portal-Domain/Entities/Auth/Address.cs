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
        public AppUser user { get; set; }
        public string userId { get; set; }
    }
}
