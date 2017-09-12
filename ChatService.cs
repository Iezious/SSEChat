using System;
using System.Collections.Concurrent;

namespace SEEChat
{
    public interface IChatService
    {
        void Register(string name, Action<string, string> callback);
        void Unregister(string name);
        void Message(string name, string message);
    }
    
    
    public class ChatService : IChatService
    {
        private ConcurrentDictionary<string, Action<string, string>> _waiters = new ConcurrentDictionary<string, Action<string, string>>();

        public void Register(string name, Action<string, string> callback)
        {
            _waiters[name] = callback;
            Message("System", $"{name} joined the chat");
        }

        public void Unregister(string name)
        {
            Message("System", $"{name} left the chat");
        }

        public void Message(string name, string message)
        {
            foreach (var waiter in _waiters.Values)
            {
                waiter(name, message);
            }
        }
    }
}