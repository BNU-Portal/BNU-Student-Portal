using System;
using System.Collections.Generic;
using System.Text;

namespace YourAPP_Domain.Entities
{
    public class TestFeatEntity : BaseEntity<int>
    {
        public  string Name { get; set; } = string.Empty!;
    }
}
