using System;
using System.Collections.Generic;

namespace EdgeCases
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly List<string> _sentNotifications = new();
        private bool _disposed = false;

        public bool IsEnabled { get; set; } = true;

        public void SendNotification(string message)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NotificationService));
                
            if (!IsEnabled)
                return;

            _sentNotifications.Add($"{DateTime.Now:HH:mm:ss}: {message}");
            Console.WriteLine($"Notification sent: {message}");
        }

        public string[] GetSentNotifications()
        {
            return _sentNotifications.ToArray();
        }

        public void ClearNotifications()
        {
            _sentNotifications.Clear();
        }

        public int GetNotificationCount()
        {
            return _sentNotifications.Count;
        }

        public void SendBulkNotifications(string[] messages)
        {
            foreach (var message in messages)
            {
                SendNotification(message);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _sentNotifications.Clear();
                _disposed = true;
                Console.WriteLine("NotificationService disposed");
            }
        }
    }
}