namespace BNU_Student_Portal_Services.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string message)
        : base(message) { }
}
