using System;
using System.Linq;

namespace Klinkby.TimerJob
{
    public class Job : IJob
    {
        private static readonly Tuple<Periodicity, long>[] TicksMap =
        {
            new Tuple<Periodicity, long>(Periodicity.Minute, TimeSpan.FromMinutes(1).Ticks),
            new Tuple<Periodicity, long>(Periodicity.Hour, TimeSpan.FromHours(1).Ticks),
            new Tuple<Periodicity, long>(Periodicity.Day, TimeSpan.FromDays(1).Ticks)
        };

        private readonly Action _command;
        private readonly Periodicity _periodicity;
        private readonly IJobPersister _persister;
        private readonly int _repeatEvery;
        private DateTime _lastRun;

        public Job(Action command, Periodicity periodicity, int repeatEvery, IJobPersister persister)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (persister == null) throw new ArgumentNullException("persister");
            _command = command;
            _periodicity = periodicity;
            _repeatEvery = repeatEvery;
            _persister = persister;
        }

        public Periodicity Periodicity
        {
            get { return _periodicity; }
        }

        public int RepeatEvery
        {
            get { return _repeatEvery; }
        }

        public IJobPersister Persister
        {
            get { return _persister; }
        }

        public bool IsDue
        {
            get
            {
                DateTime nextRun = GetNextRun(_lastRun); // first try the cached value
                bool isDue = nextRun < DateTime.UtcNow;
                if (!isDue) return false;
                _lastRun = _persister.LastRun;
                nextRun = GetNextRun(_lastRun); // next try the db value
                isDue = nextRun < DateTime.UtcNow;
                return isDue && _persister.TrySetRunFlag(_lastRun, nextRun);
            }
        }

        public void Run()
        {
            _command();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private DateTime GetNextRun(DateTime lastRun)
        {
            long period = TicksMap.First(x => Periodicity == x.Item1).Item2*RepeatEvery;
            DateTime now = DateTime.UtcNow;
            DateTime nextRun = lastRun + new TimeSpan(period);
            bool missedRuns = now - new TimeSpan(period) > nextRun;
            if (missedRuns)
            {
                long runsMissed = (now - lastRun).Ticks/period;
                DateTime lastRunMissed = lastRun + new TimeSpan(runsMissed*period);
                nextRun = lastRunMissed;
            }
            return nextRun;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _persister.Dispose();
            }
        }
    }
}