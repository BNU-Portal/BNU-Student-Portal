namespace YourAPP_Services.Exceptions;

public class ServiceUnavailableException : Exception
{
    // Use this when you have a full custom message
    public ServiceUnavailableException(string message)
        : base(message) { }

    // Use this when you only have the service name
    // e.g. throw new ServiceUnavailableException("PaymentService", isServiceName: true)
    public ServiceUnavailableException(string serviceName, bool isServiceName)
        : base($"Service '{serviceName}' is currently unavailable. Please try again later.") { }
}
