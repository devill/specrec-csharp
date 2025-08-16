using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public class NotificationServiceWrapper : INotificationServiceWrapper
    {
        private readonly NotificationService _wrapped;

        public NotificationServiceWrapper(NotificationService wrapped)
        {
            _wrapped = wrapped;
        }

        public bool IsEnabled { get => _wrapped.IsEnabled; set => _wrapped.IsEnabled = value; }

        public void SendNotification(string message)
        {
            _wrapped.SendNotification(message);
        }

        public string[] GetSentNotifications()
        {
            return _wrapped.GetSentNotifications();
        }

        public void ClearNotifications()
        {
            _wrapped.ClearNotifications();
        }

        public int GetNotificationCount()
        {
            return _wrapped.GetNotificationCount();
        }

        public void SendBulkNotifications(string[] messages)
        {
            _wrapped.SendBulkNotifications(messages);
        }

        public void Dispose()
        {
            _wrapped.Dispose();
        }
    }
}