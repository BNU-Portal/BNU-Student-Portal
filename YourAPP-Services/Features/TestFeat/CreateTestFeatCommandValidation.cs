using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Text;

namespace YourAPP_Services.Features.TestFeat
{
    public class CreateTestFeatCommandValidation : AbstractValidator<CreateTestFeatCommand>
    {

        public CreateTestFeatCommandValidation()
        {
            //RuleFor(x => x.id).NotEmpty().GreaterThan(0);
            RuleFor(x => x.name).NotEmpty().MaximumLength(100);
        }

    }
}
