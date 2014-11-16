using System;
using System.Threading;

namespace Klinkby.TimerJob
{
    internal sealed class ImpulseGenerator : IDisposable
    {
        private readonly Timer _timer;

        public ImpulseGenerator(TimeSpan interval, EventWaitHandle signal)
        {
            _timer = new Timer(Timer_Tick, signal, interval, interval);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private static void Timer_Tick(object state)
        {
            var signal = (EventWaitHandle) state;
            signal.Set();
        }

        public void Dispose(WaitHandle notifyObject)
        {
            _timer.Dispose(notifyObject);
        }
    }
}