namespace Disney.WebAPI.Infrastructure.Email
{
    public interface ISendEmail
    {
        Task SendMail(string to, string subject, string body);
    }
}
