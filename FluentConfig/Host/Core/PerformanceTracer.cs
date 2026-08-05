using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

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
        private readonly List<(string Name, long Ms, string Kind)> _milestones =
            new List<(string, long, string)>();
        private readonly Action<string> _log;
#if FC_PERF_TRACE
        private static int _openSequence;
        private string _currentPhase;
        private bool _summaryLogged;
        private bool _cold;
#endif

        public PerformanceTracer(Action<string> log)
        {
            _log = log;
        }

        /// <summary>True when this Start() is the first in the process (cold open).</summary>
        public bool IsCold
        {
            get
            {
#if FC_PERF_TRACE
                return _cold;
#else
                return false;
#endif
            }
        }

        public void Start(string firstPhase)
        {
#if FC_PERF_TRACE
            _summaryLogged = false;
            _milestones.Clear();
            _cold = Interlocked.Increment(ref _openSequence) == 1;
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
                _milestones.Add((_currentPhase, elapsed, "phase"));
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
                _milestones.Add((_currentPhase, elapsed, "phase"));
                _log?.Invoke($"[PerfTrace] {_currentPhase}: {elapsed}ms");
                _currentPhase = null;
                _phase.Stop();
            }
#endif
        }

        /// <summary>
        /// Record a named mark at the current total elapsed time (does not end the active phase).
        /// Used for web → host <c>perf.mark</c> milestones.
        /// </summary>
        public void Mark(string name)
        {
#if FC_PERF_TRACE
            if (string.IsNullOrWhiteSpace(name)) return;
            var elapsed = _total.ElapsedMilliseconds;
            _milestones.Add((name, elapsed, "mark"));
            _log?.Invoke($"[PerfTrace] mark {name}: {elapsed}ms");
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
            _log?.Invoke($"  cold={_cold}");
            foreach (var (name, ms, kind) in _milestones)
            {
                var label = kind == "mark" ? $"mark:{name}" : name;
                _log?.Invoke($"  {label,-40} {ms,6}ms");
            }
            _log?.Invoke($"  {"TOTAL",-40} {_total.ElapsedMilliseconds,6}ms");
            _log?.Invoke("=========================================");
#endif
        }

        /// <summary>
        /// Machine-readable last-run summary for CPH <c>FluentConfig_PerfLast</c>.
        /// Returns null unless <see cref="LogSummary"/> has completed for this Start cycle.
        /// </summary>
        public string ToSummaryJson()
        {
#if FC_PERF_TRACE
            if (!_summaryLogged) return null;

            var sb = new StringBuilder(256 + _milestones.Count * 48);
            sb.Append("{\"cold\":");
            sb.Append(_cold ? "true" : "false");
            sb.Append(",\"totalMs\":");
            sb.Append(_total.ElapsedMilliseconds);
            sb.Append(",\"milestones\":[");
            for (var i = 0; i < _milestones.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var (name, ms, kind) = _milestones[i];
                sb.Append("{\"name\":");
                AppendJsonString(sb, name);
                sb.Append(",\"ms\":");
                sb.Append(ms);
                sb.Append(",\"kind\":\"");
                sb.Append(kind);
                sb.Append("\"}");
            }
            sb.Append("]}");
            return sb.ToString();
#else
            return null;
#endif
        }

        public long TotalMs => _total.ElapsedMilliseconds;

#if FC_PERF_TRACE
        private static void AppendJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                foreach (var c in value)
                {
                    switch (c)
                    {
                        case '\\': sb.Append("\\\\"); break;
                        case '"': sb.Append("\\\""); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < ' ')
                                sb.Append("\\u").Append(((int)c).ToString("x4"));
                            else
                                sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }
#endif
    }
}
