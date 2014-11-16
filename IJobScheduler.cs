using System;

namespace Klinkby.TimerJob
{
    public interface IJobScheduler
    {
        DateTime NextRun { get; }
        bool IsDue { get; }
        void RunScheduled(DateTime jobTime);
        void RunComplete(string status);
    }
}