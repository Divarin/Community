using miniBBS.Core.Models.Data;
using System.Collections.Generic;

namespace miniBBS.Core.Interfaces
{
    /// <summary>
    /// Handles notification sending/fetching only for offline messages, does not notifiy users in real-time.
    /// </summary>
    public interface INotificationHandler
    {
        void SendNotification(int userId, string notification);
        IEnumerable<Notification> GetNotifications(int userId);
    }
}
