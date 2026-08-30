using System;
using System.Collections.Generic;
using System.Linq;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>Dropdown refresh, button.click, and pill.changed RPC handlers.</summary>
    public sealed partial class FluentConfigSession
    {
        private void HandleDropdownRefresh(WireMessage msg)
        {
            var p = msg.Params?.ToObject<DropdownRefreshParams>(ProtocolJson.CreateSerializer());
            IList<DropdownOption> options = Array.Empty<DropdownOption>();
            try
            {
                if (p != null
                    && !string.IsNullOrEmpty(p.SaveKey)
                    && ButtonClickRouting.TryResolve(_dropdownRefresh, p.SaveKey, out var fn, out _)
                    && fn != null)
                    options = fn() ?? Array.Empty<DropdownOption>();
            }
            catch (Exception ex)
            {
                Log($"[FluentConfig] dropdown.refresh failed: {ex.Message}");
            }
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new DropdownRefreshResult { Options = options }));
        }

        private void HandleButtonClick(WireMessage msg)
        {
            var p = msg.Params?.ToObject<ButtonClickParams>(ProtocolJson.CreateSerializer());
            if (p?.Values != null)
                _latestValues = p.Values;

            Action<UiContext> cb = null;
            string itemName = null;
            if (p != null && !string.IsNullOrEmpty(p.ButtonId))
                ButtonClickRouting.TryResolve(_buttonClicks, p.ButtonId, out cb, out itemName);

            // Reply before running OnClick. Nested SendRequestAndWait (confirm/popup) inside
            // WebMessageReceived deadlocks — WebView2 won't deliver the dialog response until
            // this handler returns. Defer the callback so confirm/progress RPCs can complete.
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));

            if (cb == null) return;

            var ctx = new UiContext(this, _latestValues, p.ButtonId, itemName);
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
                    try
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
                    catch (Exception ex)
                    {
                        Log($"[FluentConfig] pill.changed expand failed: {ex.Message}");
                    }
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
    }
}
