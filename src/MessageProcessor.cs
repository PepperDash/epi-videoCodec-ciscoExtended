using System;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace epi_videoCodec_ciscoExtended
{
    public class MessageProcessor
    {
        private readonly IKeyed _parent;
        private const int Idle = 0;
        private const int InProgress = 1;

        private int _status;

        private readonly BlockingUnboundedQueue<Action> _messages
            = new BlockingUnboundedQueue<Action>();


        public MessageProcessor(IKeyed parent)
        {
            _parent = parent;
        }

        public void PostMessage(Action message)
        {
            _messages.Push(message);
            Schedule();
        }

        private void Schedule()
        {
            if (Interlocked.CompareExchange(ref _status, InProgress, Idle) == Idle)
                RunAsync();
        }

        private void RunAsync()
        {
            CrestronInvoke.BeginInvoke(_ =>
            {
                ProcessMessages();
                Interlocked.Exchange(ref _status, Idle);

                if (_messages.HasItems)
                    Schedule();
            });
        }

        private void ProcessMessages()
        {
            const int throughput = 5;
            for (var i = 0; i < throughput; ++i)
            {
                var message = _messages.Pop();
                try
                {
                    if (message != null)
                        message();
                }
                catch (Exception ex)
                {
                    Debug.Console(1, _parent, "Error processing message:{0}", ex);
                }
            }
        }
    }
}