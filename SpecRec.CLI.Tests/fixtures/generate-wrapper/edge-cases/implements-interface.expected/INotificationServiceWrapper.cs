using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public interface INotificationServiceWrapper
    {
        // From INotificationService
        bool IsEnabled { get; set; }
        void SendNotification(string message);

        // From IDisposable
        void Dispose();

        // Additional public methods from NotificationService
        string[] GetSentNotifications();
        void ClearNotifications();
        int GetNotificationCount();
        void SendBulkNotifications(string[] messages);
    }
}