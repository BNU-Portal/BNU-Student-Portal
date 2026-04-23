using System;
using System.Collections.Generic;
using System.Text;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Auth
{
    public record RefreshTokenDto(
        
        string AccessToken,
        string RefreshToken


        );
}
