using miniBBS.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace miniBBS.Services.Services
{
    public class Messager : IMessager
    {
        private readonly ConcurrentDictionary<Type, object> _subscribers = new ConcurrentDictionary<Type, object>();
        private object _lock = new object();

        public void Publish<TMessage>(TMessage message)
            where TMessage : IMessage
        {
            lock (_lock)
            {
                var set = GetSet<TMessage>();
                foreach (var subscriber in set)
                    subscriber.Receive(message);
            }
        }

        public void Subscribe<TMessage>(ISubscriber<TMessage> subscriber)
            where TMessage : IMessage
        {
            lock (_lock)
            {
                var set = GetSet<TMessage>();
                set.Add(subscriber);
            }
        }

        public void Unsubscribe<TMessage>(ISubscriber<TMessage> subscriber)
            where TMessage : IMessage
        {
            if (subscriber == null)
                return;

            lock (_lock)
            {
                var set = GetSet<TMessage>();
                if (set.Contains(subscriber))
                    set.Remove(subscriber);
            }
        }

        private HashSet<ISubscriber<TMessage>> GetSet<TMessage>()
            where TMessage : IMessage
        {
            Type type = typeof(TMessage);

            if (_subscribers.ContainsKey(type))
                return (HashSet<ISubscriber<TMessage>>)_subscribers[type];

            var set = new HashSet<ISubscriber<TMessage>>();
            _subscribers[type] = set;
            return set;
        }
    }
}
