using System;

namespace Klinkby.TimerJob
{
    public interface IJob : IDisposable
    {
        bool IsDue { get; }
        void Run();
    }
}