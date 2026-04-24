using BNU_Student_Portal_Shared_Library.SharedResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BNU_Student_Portal_Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiBaseController : ControllerBase
    {
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return NoContent();

            return HandleProblem(result.Errors);
        }

        protected ActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.GetValueOrThrow());

            return HandleProblem(result.Errors);
        }

        private ActionResult HandleProblem(IReadOnlyCollection<Error> errors)
        {
            if (errors.Count == 0)
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.");

            if (errors.All(e => e.Type == ErrorType.Validation))
                return HandleValidationProblem(errors);

            return HandleSingleProblem(errors.First());
        }

        private ActionResult HandleSingleProblem(Error error)
        {
            return Problem(
                title: error.Code,
                detail: error.Description,
                type: error.Type.ToString(),
                statusCode: GetStatusCode(error.Type));
        }

        private static int GetStatusCode(ErrorType errorType) => errorType switch
        {
            ErrorType.NotFound            => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized        => StatusCodes.Status401Unauthorized,
            ErrorType.InvalidCredentials  => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden           => StatusCodes.Status403Forbidden,
            ErrorType.Validation          => StatusCodes.Status422UnprocessableEntity,
            ErrorType.BadRequest          => StatusCodes.Status400BadRequest,
            ErrorType.InternalServerError => StatusCodes.Status500InternalServerError,
            _                             => StatusCodes.Status500InternalServerError
        };

        private ActionResult HandleValidationProblem(IReadOnlyCollection<Error> errors)
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
                modelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(modelState);
        }
    }
}
