namespace BNU_Student_Portal_Services_Implementation;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true);
}