using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    public class BlockingUnboundedQueue<T>
    {
        private readonly object _lock = new object();
        private readonly LinkedList<T> _queue;
        private readonly IKeyed _parent;

        public BlockingUnboundedQueue(IKeyed parent)
        {
            _parent = parent;
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
                catch (Exception ex)
                {
                    Debug.Console(1, _parent, Debug.ErrorLogLevel.Notice, "Error checking message:{0}", ex);
                    throw;
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
            catch (Exception ex)
            {
                Debug.Console(1, _parent, Debug.ErrorLogLevel.Notice, "Error pushing message:{0}", ex);
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
            catch (Exception ex)
            {
                Debug.Console(1, _parent, Debug.ErrorLogLevel.Notice, "Error popping message:{0}", ex);
                throw;
            }
            finally
            {
                CMonitor.Exit(_lock);
            }
        }
    }
}