using FluentValidation;
using MediatR;

namespace YourAPP_Services.FluentValidationMiddleWare
{
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {










        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            //if there is no validation errors then we will continue to the next behavior in the pipeline

            if(!validators.Any())
            {
                return await next(); //gonna call the next behavior in the pipeline , which can be a command or a query handler
            }

            //"Wrap my CreateProductCommand inside a box
            //that FluentValidation's Validate() method accepts"
            //so it actually wraps up the command or query that we are sending to the handler and then it
            //will validate it in a form that validate function take as a parameter
            var context = new ValidationContext<TRequest>(request);

            //check for the validation errors

            var errors = validators
                .Select(v => v.Validate(context)) //validate the command or query that
               //we are sending to the handler and then it will validate it in a form that validate function take as a parameter
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();


            if (errors.Count != 0)
            {
                throw new ValidationException(errors); //we gonna replace that when we add the result pattern 
            }

            return await next();
        }
    }
}
