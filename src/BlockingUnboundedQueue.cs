using System;
using System.Collections.Generic;
using System.ComponentModel;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Core.Logging;

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
                    _parent.LogError("Error checking message: {message}", ex.Message);
                    _parent.LogVerbose(ex, "Exception");
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
                _parent.LogError("Error pushing message: {message}", ex.Message);
                _parent.LogVerbose(ex, "Exception");
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
                _parent.LogError("Error popping message: {message}", ex.Message);
                _parent.LogVerbose(ex, "Exception");
                throw;
            }
            finally
            {
                CMonitor.Exit(_lock);
            }
        }
    }
}