using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services.Services
{
    public class NotificationHandler : INotificationHandler
    {
        private readonly IRepository<Notification> _repo;

        public NotificationHandler()
        {
            _repo = GlobalDependencyResolver.Default.GetRepository<Notification>();
        }

        public IEnumerable<Notification> GetNotifications(int userId)
        {
            var results = _repo.Get(n => n.UserId, userId)
                ?.OrderBy(n => n.DateSentUtc)
                ?.ToList();

            if (true == results?.Any())
            {
                _repo.DeleteRange(results);
            }

            return results;
        }

        public void SendNotification(int userId, string notification)
        {
            _repo.Insert(new Notification
            {
                UserId = userId,
                DateSentUtc = DateTime.UtcNow,
                Message = notification
            });
        }
    }
}
