using System;
using System.Diagnostics;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>shell.openUrl, perf.mark, and CPH perf-summary export.</summary>
    public sealed partial class FluentConfigSession
    {
        private void HandleShellOpenUrl(WireMessage msg)
        {
            var p = msg.Params?.ToObject<ShellOpenUrlParams>(ProtocolJson.CreateSerializer());
            var url = p?.Url?.Trim();
            if (string.IsNullOrEmpty(url)
                || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "bad_params", "url must be http(s)"));
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] shell.openUrl failed: {ex.Message}");
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
        }

        private void HandlePerfMark(WireMessage msg)
        {
            var p = msg.Params?.ToObject<PerfMarkParams>(ProtocolJson.CreateSerializer());
            var name = p?.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                _perfTracer.Mark(name);
                if (string.Equals(name, "web-ready", StringComparison.OrdinalIgnoreCase))
                {
                    _perfTracer.LogSummary();
                    ExportPerfLastToCph();
                }
            }
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
        }

        /// <summary>
        /// Writes the last PerfTrace summary to CPH global <c>FluentConfig_PerfLast</c>
        /// (no-op unless compiled with FC_PERF_TRACE and summary has closed).
        /// </summary>
        private void ExportPerfLastToCph()
        {
#if FC_PERF_TRACE
            try
            {
                var json = _perfTracer.ToSummaryJson();
                if (json == null) return;
                _cph.SetGlobalVar("FluentConfig_PerfLast", json, true);
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] FluentConfig_PerfLast export failed: {ex.Message}");
            }
#endif
        }
    }
}
