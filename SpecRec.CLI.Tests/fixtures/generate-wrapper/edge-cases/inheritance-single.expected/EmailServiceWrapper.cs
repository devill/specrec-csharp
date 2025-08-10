using System;

namespace EdgeCases
{
    public class EmailServiceWrapper : IEmailService
    {
        private readonly EmailService _wrapped;

        public EmailServiceWrapper(EmailService wrapped)
        {
            _wrapped = wrapped;
        }

        // Inherited from BaseService
        public bool IsConnected => _wrapped.IsConnected;

        public void Initialize(string connectionString)
        {
            _wrapped.Initialize(connectionString);
        }

        public void Dispose()
        {
            _wrapped.Dispose();
        }

        // EmailService specific methods
        public void SendEmail(string to, string subject, string body)
        {
            _wrapped.SendEmail(to, subject, body);
        }

        public void SendBulkEmails(string[] recipients, string subject, string body)
        {
            _wrapped.SendBulkEmails(recipients, subject, body);
        }

        public bool ValidateEmailAddress(string email)
        {
            return _wrapped.ValidateEmailAddress(email);
        }

        public void ConfigureSmtp(string server, int port = 587)
        {
            _wrapped.ConfigureSmtp(server, port);
        }

        public string GetSmtpConfiguration()
        {
            return _wrapped.GetSmtpConfiguration();
        }
    }
}