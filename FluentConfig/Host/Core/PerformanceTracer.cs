using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FluentConfig.Core
{
    /// <summary>
    /// Lightweight Stopwatch-based tracer for profiling FluentConfig startup phases.
    /// When enabled via SetLogCallback, writes "[PerfTrace] phase: Nms" lines.
    /// </summary>
    public sealed class PerformanceTracer
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

        public void Start(string firstPhase)
        {
            _total.Restart();
            BeginPhase(firstPhase);
        }

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

        public void LogSummary()
        {
            _total.Stop();
            if (_currentPhase != null)
                EndPhase();

            _log?.Invoke("=== FluentConfig Startup Performance Summary ===");
            foreach (var (name, ms) in _milestones)
            {
                _log?.Invoke($"  {name,-40} {ms,6}ms");
            }
            _log?.Invoke($"  {"TOTAL",-40} {_total.ElapsedMilliseconds,6}ms");
            _log?.Invoke("=========================================");
        }

        public long TotalMs => _total.ElapsedMilliseconds;
    }
}
