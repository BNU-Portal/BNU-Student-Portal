using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace YourAPP_Services.Features.TestFeat
{
    public record CreateTestFeatCommand(int id, string name) : IRequest<int>;
}
