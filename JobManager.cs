using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Klinkby.TimerJob
{
    public class JobManager : MarshalByRefObject, IDisposable
    {
        private readonly ImpulseGenerator _impulseGenerator;
        private readonly IProducerConsumerCollection<IJob> _jobs;
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly WorkerThread _workerThread;

        public JobManager(IProducerConsumerCollection<IJob> jobs, TimeSpan resolution)
        {
            _jobs = jobs;
            _workerThread = new WorkerThread(_signal, _jobs);
            _workerThread.Error += WorkerThread_OnError;
            _impulseGenerator = new ImpulseGenerator(resolution, _signal);
        }

        public IProducerConsumerCollection<IJob> Jobs
        {
            get { return _jobs; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose of the resources and wait for them to terminate
                using (var threadDisposed = new AutoResetEvent(false))
                {
                    using (var timerDisposed = new AutoResetEvent(false))
                    {
                        _impulseGenerator.Dispose(timerDisposed);
                        _workerThread.Dispose(threadDisposed);
                        WaitHandle.WaitAll(new WaitHandle[]
                        {
                            threadDisposed,
                            timerDisposed
                        });
                    }
                }
                _signal.Dispose();
            }
        }

        private void WorkerThread_OnError(object sender, ErrorEventArgs errorEventArgs)
        {
            EventHandler<ErrorEventArgs> eh = Error;
            if (null == eh) return;
            eh(this, errorEventArgs);
        }

        public event EventHandler<ErrorEventArgs> Error;
    }
}