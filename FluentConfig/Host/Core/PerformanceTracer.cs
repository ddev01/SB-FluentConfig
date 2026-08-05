using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FluentConfig.Core
{
    /// <summary>
    /// Lightweight Stopwatch-based tracer for profiling FluentConfig startup phases.
    /// Bodies compile away unless <c>FC_PERF_TRACE</c> is defined (see Host.csproj FluentConfigPerfTrace).
    /// When enabled via SetLogCallback, writes "[PerfTrace] phase: Nms" lines.
    /// </summary>
    public sealed class PerformanceTracer
    {
        private readonly Stopwatch _total = new Stopwatch();
        private readonly Stopwatch _phase = new Stopwatch();
        private readonly List<(string Name, long Ms)> _milestones = new List<(string, long)>();
        private readonly Action<string> _log;
#if FC_PERF_TRACE
        private string _currentPhase;
        private bool _summaryLogged;
#endif

        public PerformanceTracer(Action<string> log)
        {
            _log = log;
        }

        public void Start(string firstPhase)
        {
#if FC_PERF_TRACE
            _summaryLogged = false;
            _milestones.Clear();
            _total.Restart();
            BeginPhase(firstPhase);
#endif
        }

        public void BeginPhase(string name)
        {
#if FC_PERF_TRACE
            if (_currentPhase != null)
            {
                var elapsed = _phase.ElapsedMilliseconds;
                _milestones.Add((_currentPhase, elapsed));
                _log?.Invoke($"[PerfTrace] {_currentPhase}: {elapsed}ms");
            }
            _currentPhase = name;
            _phase.Restart();
#endif
        }

        public void EndPhase()
        {
#if FC_PERF_TRACE
            if (_currentPhase != null)
            {
                var elapsed = _phase.ElapsedMilliseconds;
                _milestones.Add((_currentPhase, elapsed));
                _log?.Invoke($"[PerfTrace] {_currentPhase}: {elapsed}ms");
                _currentPhase = null;
                _phase.Stop();
            }
#endif
        }

        public void LogSummary()
        {
#if FC_PERF_TRACE
            if (_summaryLogged) return;
            _summaryLogged = true;
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
#endif
        }

        public long TotalMs => _total.ElapsedMilliseconds;
    }
}
