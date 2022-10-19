using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace epi_videoCodec_ciscoExtended
{
    public class BlockingUnboundedQueue<T>
    {
        private readonly object _lock = new object();
        private readonly LinkedList<T> _queue;

        public BlockingUnboundedQueue()
        {
            _queue = new LinkedList<T>();
        }

        public bool HasItems
        {
            get
            {
                CMonitor.Enter(_lock);
                try
                {
                    return _queue.Count > 0;
                }
                finally
                {
                    CMonitor.Exit(_lock);
                }
            }
        }

        public void Push(T message)
        {
            CMonitor.Enter(_lock);
            try
            {
                _queue.AddLast(message);
            }
            finally
            {
                CMonitor.Exit(_lock);
            }
        }

        public T Pop()
        {
            var result = default(T);
            if (!HasItems)
                return result;

            CMonitor.Enter(_lock);
            try
            {
                if (_queue.Count == 0)
                    return result;

                result = _queue.First.Value;
                _queue.RemoveFirst();
                return result;
            }
            finally
            {
                CMonitor.Exit(_lock);
            }
        }
    }
}