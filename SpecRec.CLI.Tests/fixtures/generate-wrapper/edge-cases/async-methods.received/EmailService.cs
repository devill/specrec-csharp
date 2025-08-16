using System;

namespace EdgeCases
{
    public class EmailService : BaseService
    {
        private string _smtpServer = "";
        private int _port = 587;

        public override void Initialize(string connectionString)
        {
            base.Initialize(connectionString);
            // Parse SMTP server and port from connection string
            var parts = connectionString.Split(':');
            if (parts.Length >= 2)
            {
                _smtpServer = parts[0];
                if (int.TryParse(parts[1], out var port))
                {
                    _port = port;
                }
            }
        }

        public override bool IsConnected => base.IsConnected && !string.IsNullOrEmpty(_smtpServer);

        public void SendEmail(string to, string subject, string body)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Email service not connected");
            }
            
            Console.WriteLine($"Sending email to {to}: {subject}");
            Console.WriteLine($"Using SMTP server: {_smtpServer}:{_port}");
        }

        public void SendBulkEmails(string[] recipients, string subject, string body)
        {
            foreach (var recipient in recipients)
            {
                SendEmail(recipient, subject, body);
            }
        }

        public bool ValidateEmailAddress(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && email.Contains("@");
        }

        public void ConfigureSmtp(string server, int port = 587)
        {
            _smtpServer = server;
            _port = port;
        }

        public string GetSmtpConfiguration()
        {
            return $"{_smtpServer}:{_port}";
        }
    }
}