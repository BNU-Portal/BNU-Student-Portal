using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Auth
{
    public class LoginReturnDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
