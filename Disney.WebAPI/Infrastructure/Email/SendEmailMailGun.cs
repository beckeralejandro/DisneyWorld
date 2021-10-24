using RestSharp;
using RestSharp.Authenticators;

namespace Disney.WebAPI.Infrastructure.Email
{
    public class SendEmailMailGun : ISendEmail
    {
        public async Task SendMail(string to, string subject, string body)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator = new HttpBasicAuthenticator("api", "private");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "sandboxc69c1adeee23448fbf6514358ac480f5.mailgun.org", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "Mailgun Sandbox <postmaster@sandboxc69c1adeee23448fbf6514358ac480f5.mailgun.org>");
            request.AddParameter("to", to);
            request.AddParameter("subject", subject);
            request.AddParameter("html", body);
            request.Method = Method.POST;
            await client.ExecuteAsync(request);
        }
    }
}
