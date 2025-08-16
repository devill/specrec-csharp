using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public interface INotificationServiceWrapper
    {
        // From INotificationService
        void SendNotification(string message);
        bool IsEnabled { get; set; }

        // From IDisposable
        void Dispose();
        // Additional public methods from NotificationService
        string[] GetSentNotifications();
        void ClearNotifications();
        int GetNotificationCount();
        void SendBulkNotifications(string[] messages);
    }
}