using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sbui.Core
{
    /// <summary>
    /// Lightweight Stopwatch-based tracer for profiling Sbui startup phases.
    /// When enabled via SetLogCallback, writes "[PerfTrace] phase: Nms" lines.
    /// Accumulates milestones so a full summary can be logged at the end.
    /// Thread-safe for read access; typical usage is single-threaded (UI thread).
    /// </summary>
    internal sealed class PerformanceTracer
    {
        private readonly Stopwatch _total = new Stopwatch();
        private readonly Stopwatch _phase = new Stopwatch();
        private readonly List<(string Name, long Ms)> _milestones = new List<(string, long)>();
        private readonly Action<string> _log;
        private string _currentPhase;

        public PerformanceTracer(Action<string> log)
        {
            _log = log;
        }

        /// <summary>
        /// Starts overall timing and the first phase.
        /// </summary>
        public void Start(string firstPhase)
        {
            _total.Restart();
            BeginPhase(firstPhase);
        }

        /// <summary>
        /// Ends the current phase (logging its duration) and begins a new one.
        /// </summary>
        public void BeginPhase(string name)
        {
            if (_currentPhase != null)
            {
                var elapsed = _phase.ElapsedMilliseconds;
                _milestones.Add((_currentPhase, elapsed));
                _log?.Invoke($"[PerfTrace] {_currentPhase}: {elapsed}ms");
            }
            _currentPhase = name;
            _phase.Restart();
        }

        /// <summary>
        /// Ends the current phase without starting a new one. Use before LogSummary.
        /// </summary>
        public void EndPhase()
        {
            if (_currentPhase != null)
            {
                var elapsed = _phase.ElapsedMilliseconds;
                _milestones.Add((_currentPhase, elapsed));
                _log?.Invoke($"[PerfTrace] {_currentPhase}: {elapsed}ms");
                _currentPhase = null;
                _phase.Stop();
            }
        }

        /// <summary>
        /// Logs a full summary table of all phases plus the total.
        /// </summary>
        public void LogSummary()
        {
            _total.Stop();
            if (_currentPhase != null)
                EndPhase();

            _log?.Invoke("=== Sbui Startup Performance Summary ===");
            foreach (var (name, ms) in _milestones)
            {
                _log?.Invoke($"  {name,-40} {ms,6}ms");
            }
            _log?.Invoke($"  {"TOTAL",-40} {_total.ElapsedMilliseconds,6}ms");
            _log?.Invoke("=========================================");
        }

        /// <summary>
        /// Total elapsed milliseconds since Start().
        /// </summary>
        public long TotalMs => _total.ElapsedMilliseconds;
    }
}
