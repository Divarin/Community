using System;

namespace miniBBS.Core.Interfaces
{
    /// <summary>
    /// Messager handles pub/sub ops for node-to-node communication
    /// </summary>
    public interface IMessager
    {
        void Subscribe<TMessage>(ISubscriber<TMessage> subscriber)
            where TMessage : IMessage;
        void Unsubscribe<TMessage>(ISubscriber<TMessage> subscriber)
            where TMessage : IMessage;
        void Publish<TMessage>(TMessage message)
            where TMessage : IMessage;
    }

    public interface ISubscriber<TMessage>
        where TMessage : IMessage
    {
        void Receive(TMessage message);
        Action<TMessage> OnMessageReceived { get; set; }
    }

    public interface IMessage
    {
        /// <summary>
        /// The ID of the session that published the message
        /// </summary>
        Guid SessionId { get; }
    }

}
