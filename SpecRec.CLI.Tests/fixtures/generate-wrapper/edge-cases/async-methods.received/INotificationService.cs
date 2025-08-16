using System;

namespace EdgeCases
{
    public interface INotificationService
    {
        void SendNotification(string message);
        bool IsEnabled { get; set; }
    }

    public interface IDisposable
    {
        void Dispose();
    }
}