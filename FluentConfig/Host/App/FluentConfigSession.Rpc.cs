using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Updater;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>Host bridge RPC dispatch and request handlers.</summary>
    public sealed partial class FluentConfigSession
    {
        internal void HandleWebMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Log("[FluentConfig] Empty web message (expected JSON string from postMessage)");
                return;
            }

            WireMessage msg;
            try { msg = ProtocolJson.Deserialize<WireMessage>(json); }
            catch (Exception ex)
            {
                Log($"[FluentConfig] Bad wire message: {ex.Message}");
                return;
            }
            if (msg == null) return;

            if (msg.Kind == WireKinds.Request)
                HandleRequest(msg);
            else if (msg.Kind == WireKinds.Response)
                _bridge?.CompletePendingResponse(msg);
        }

        private void HandleRequest(WireMessage msg)
        {
            if (msg.Id == null)
            {
                Log("[FluentConfig] Request missing id");
                return;
            }

            try
            {
                switch (msg.Method)
                {
                    case RpcMethods.Save:
                        HandleSave(msg);
                        break;
                    case RpcMethods.DropdownRefresh:
                        HandleDropdownRefresh(msg);
                        break;
                    case RpcMethods.ButtonClick:
                        HandleButtonClick(msg);
                        break;
                    case RpcMethods.FilepathBrowse:
                        HandleFilepathBrowse(msg);
                        break;
                    case RpcMethods.PillChanged:
                        HandlePillChanged(msg);
                        break;
                    case RpcMethods.UpdateStage:
                        HandleUpdateStage(msg);
                        break;
                    case RpcMethods.UpdateDismiss:
                        HandleUpdateDismissRpc(msg);
                        break;
                    case RpcMethods.WindowClose:
                        HandleWindowClose(msg);
                        break;
                    case RpcMethods.ShellOpenUrl:
                        HandleShellOpenUrl(msg);
                        break;
                    case RpcMethods.PerfMark:
                        HandlePerfMark(msg);
                        break;
                    default:
                        _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "method_not_found", "Unknown method: " + msg.Method));
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] RPC '{msg.Method}' failed: {ex.Message}");
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "internal", ex.Message));
            }
        }

        private void HandleSave(WireMessage msg)
        {
            var p = msg.Params?.ToObject<SaveParams>(ProtocolJson.CreateSerializer());
            _latestValues = SettingsSync.ApplyAndSave(_settingsManager, p?.Values);
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new { ok = true }));
        }

        private void HandleDropdownRefresh(WireMessage msg)
        {
            var p = msg.Params?.ToObject<DropdownRefreshParams>(ProtocolJson.CreateSerializer());
            IList<DropdownOption> options = Array.Empty<DropdownOption>();
            if (p != null && !string.IsNullOrEmpty(p.SaveKey) && _dropdownRefresh.TryGetValue(p.SaveKey, out var fn))
                options = fn() ?? Array.Empty<DropdownOption>();
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new DropdownRefreshResult { Options = options }));
        }

        private void HandleButtonClick(WireMessage msg)
        {
            var p = msg.Params?.ToObject<ButtonClickParams>(ProtocolJson.CreateSerializer());
            if (p?.Values != null)
                _latestValues = p.Values;

            Action<UiContext> cb = null;
            if (p != null && !string.IsNullOrEmpty(p.ButtonId))
                _buttonClicks.TryGetValue(p.ButtonId, out cb);

            // Reply before running OnClick. Nested SendRequestAndWait (confirm/popup) inside
            // WebMessageReceived deadlocks — WebView2 won't deliver the dialog response until
            // this handler returns. Defer the callback so confirm/progress RPCs can complete.
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));

            if (cb == null) return;

            var ctx = new UiContext(this, _latestValues);
            var dispatcher = _window?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        cb(ctx);
                    }
                    catch (Exception ex)
                    {
                        Log($"[FluentConfig] button.click handler failed: {ex.Message}");
                    }
                }));
            }
            else
            {
                try
                {
                    cb(ctx);
                }
                catch (Exception ex)
                {
                    Log($"[FluentConfig] button.click handler failed: {ex.Message}");
                }
            }
        }

        private void HandleFilepathBrowse(WireMessage msg)
        {
            string path = null;
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog(_window) == true)
                path = dlg.FileName;
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new FilepathBrowseResult { Path = path }));
        }

        private void HandlePillChanged(WireMessage msg)
        {
            var p = msg.Params?.ToObject<PillChangedParams>(ProtocolJson.CreateSerializer());
            var result = new PillChangedResult();
            var ctx = new CallbackContext(this);

            Action deferredCallback = null;
            if (p != null && !string.IsNullOrEmpty(p.SaveKey) && _pills.TryGetValue(p.SaveKey, out var reg))
            {
                if (p.Action == "add" && !string.IsNullOrEmpty(p.Name) && reg.OnAdded != null)
                {
                    var name = p.Name;
                    var onAdded = reg.OnAdded;
                    deferredCallback = () => onAdded(name, ctx);
                }
                else if (p.Action == "remove" && !string.IsNullOrEmpty(p.Name) && reg.OnRemoved != null)
                {
                    var name = p.Name;
                    var onRemoved = reg.OnRemoved;
                    deferredCallback = () => onRemoved(name, ctx);
                }

                if (reg.ItemTemplate != null && p.Items != null)
                {
                    result.Items = p.Items
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Select(n => new PillItemSchema
                        {
                            Name = n,
                            Children = ExpandTemplate(reg.ItemTemplate, n),
                        })
                        .ToList();
                }
            }

            if (p?.Items != null && !string.IsNullOrEmpty(p.SaveKey))
            {
                _settingsManager.SetValue(p.SaveKey, p.Items.ToArray());
                _latestValues = _settingsManager.GetSettings();
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, result));

            // Defer author callbacks so a slow OnAdded/OnRemoved cannot block the RPC reply.
            if (deferredCallback != null)
            {
                var dispatcher = _window?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        try { deferredCallback(); }
                        catch (Exception ex)
                        {
                            Log($"[FluentConfig] pill.changed handler failed: {ex.Message}");
                        }
                    }));
                }
                else
                {
                    try { deferredCallback(); }
                    catch (Exception ex)
                    {
                        Log($"[FluentConfig] pill.changed handler failed: {ex.Message}");
                    }
                }
            }
        }

        private void HandleUpdateStage(WireMessage msg)
        {
            // Kept for tests / low-level tooling. Menu UI no longer stages self-updates;
            // FluentConfig.dll updates run from the copy-paste DllCheck action.
            var p = msg.Params?.ToObject<UpdateStageParams>(ProtocolJson.CreateSerializer());
            if (p == null || string.IsNullOrEmpty(p.DownloadUrl))
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "bad_params", "downloadUrl required"));
                return;
            }

            // Only stage the URL the host already vetted during the update check — never trust
            // an arbitrary downloadUrl from the web bundle alone.
            if (_pendingUpdate == null
                || string.IsNullOrEmpty(_pendingUpdate.DownloadUrl)
                || !string.Equals(p.DownloadUrl, _pendingUpdate.DownloadUrl, StringComparison.Ordinal))
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "bad_params", "downloadUrl does not match pending update"));
                return;
            }

            if (!GitHubUpdater.IsAllowedDownloadUrl(p.DownloadUrl))
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "bad_params", "downloadUrl host not allowed"));
                return;
            }

            try
            {
                var target = System.Reflection.Assembly.GetExecutingAssembly().Location;
                GitHubUpdater.StageUpdate(p.DownloadUrl, target);
                if (!UpdateHelperLauncher.LaunchSwapAndRelaunch(target, "Streamer.bot.exe"))
                {
                    _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "launch_failed", "UpdaterHelper did not start"));
                    return;
                }
                _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new { staged = true }));
            }
            catch (Exception ex)
            {
                Log("[FluentConfig] update.stage failed: " + ex.Message);
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "stage_failed", ex.Message));
            }
        }

        private void HandleUpdateDismissRpc(WireMessage msg)
        {
            var p = msg.Params?.ToObject<UpdateDismissParams>(ProtocolJson.CreateSerializer());
            try
            {
                HandleUpdateDismiss(p);
                _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
            }
            catch (Exception ex)
            {
                Log("[FluentConfig] update.dismiss failed: " + ex.Message);
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "dismiss_failed", ex.Message));
            }
        }

        private void HandleWindowClose(WireMessage msg)
        {
            var p = msg.Params?.ToObject<WindowCloseParams>(ProtocolJson.CreateSerializer());
            if (p != null && p.AlreadyConfirmed)
                _closeAlreadyConfirmed = true;
            if (p != null && p.DontRemindAgain)
            {
                _dontRemindDiscard = true;
                WindowPrefsStore.SetDontRemindDiscard(_cph, _title, true);
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));

            // WebMessageReceived already runs on the UI thread.
            _window?.CloseAllowed();
        }

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
