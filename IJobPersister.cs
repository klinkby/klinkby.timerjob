using System;

namespace Klinkby.TimerJob
{
    public interface IJobPersister : IDisposable
    {
        DateTime LastRun { get; }
        bool TrySetRunFlag(DateTime lastRun, DateTime nextRun);
    }
}