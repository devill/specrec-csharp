using System;

namespace EdgeCases
{
    public interface IEmailService
    {
        // Inherited from BaseService
        bool IsConnected { get; }
        void Initialize(string connectionString);
        void Dispose();

        // EmailService specific methods
        void SendEmail(string to, string subject, string body);
        void SendBulkEmails(string[] recipients, string subject, string body);
        bool ValidateEmailAddress(string email);
        void ConfigureSmtp(string server, int port = 587);
        string GetSmtpConfiguration();
    }
}