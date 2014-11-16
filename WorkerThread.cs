using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Klinkby.TimerJob
{
    internal sealed class WorkerThread : IDisposable
    {
        private readonly IProducerConsumerCollection<IJob> _jobs;
        private readonly EventWaitHandle _runSignal;
        private volatile EventWaitHandle _disposeSignal;
        private volatile bool _disposed;

        public WorkerThread(EventWaitHandle signal, IProducerConsumerCollection<IJob> jobs)
        {
            if (null == signal) throw new ArgumentNullException("signal");
            if (null == jobs) throw new ArgumentNullException("jobs");

            _runSignal = signal;
            _jobs = jobs;

            var thread = new Thread(WorkerThreadProc);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = "WorkerThread";
            thread.Start();
        }

        public void Dispose()
        {
            _disposed = true;
            _runSignal.Set();
        }

        public void Dispose(EventWaitHandle notifyObject)
        {
            _disposeSignal = notifyObject;
            Dispose();
        }

        private void WorkerThreadProc(object obj)
        {
            while (!_disposed)
            {
                _runSignal.WaitOne();
                if (_disposed) continue;
                IJob[] jobs = _jobs.ToArray(); // take a snapshot
                foreach (IJob job in jobs)
                {
                    try
                    {
                        if (job.IsDue)
                        {
                            job.Run();
                        }
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                    }
                }
            }
            if (null == _disposeSignal) return;
            _disposeSignal.Set();
        }

        public event EventHandler<ErrorEventArgs> Error;

        private void OnError(Exception e)
        {
            EventHandler<ErrorEventArgs> eh = Error;
            if (null == eh) return;
            try
            {
                eh(this, new ErrorEventArgs(e));
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
            {
                // Ignore any error from event handler
            }
        }
    }
}