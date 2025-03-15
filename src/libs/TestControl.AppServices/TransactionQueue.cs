using TestControl.Infrastructure.SubjectApiPublic;

namespace TestControl.AppServices;

internal static class TransactionQueue
{
    private static readonly Queue<UserTransaction> _queue = new();
    private static readonly Lock _locker = new();

    public static void Enqueue(UserTransaction transaction)
    {
        lock (_locker)
        {
            _queue.Enqueue(transaction);
        }
    }

    public static UserTransaction Dequeue()
    {
        lock (_locker)
        {
            if (_queue.Count > 0)
                return _queue.Dequeue();
        }
        return null;
    }

    public static UserTransaction DequeuePendingTransactionForUser(User user)
    {
        lock (_locker)
        {
            if (_queue.Count > 0)
            {
                var tempQueue = new Queue<UserTransaction>();
                UserTransaction match = null;

                while (_queue.Count > 0)
                {
                    var t = _queue.Dequeue();
                    if (!t.User.Equals(user) && t.Organization.Equals(user.Organization)
                        && t.Status.Equals("Pending"))
                    {
                        match = t;
                        break;
                    }
                    tempQueue.Enqueue(t);
                }

                while (_queue.Count > 0)
                    tempQueue.Enqueue(_queue.Dequeue());
                while (tempQueue.Count > 0)
                    _queue.Enqueue(tempQueue.Dequeue());

                return match;
            }
        }
        return null;
    }

    public static UserTransaction Peek()
    {
        lock (_locker)
        {
            if (_queue.Count > 0)
                return _queue.Peek();
        }
        return null;
    }

    public static int Count()
    {
        lock (_locker)
        {
            return _queue.Count;
        }
    }

    public static void Clear()
    {
        lock (_locker)
        {
            _queue.Clear();
        }
    }

    public static bool Contains(UserTransaction transaction)
    {
        lock (_locker)
        {
            return _queue.Contains(transaction);
        }
    }
}
