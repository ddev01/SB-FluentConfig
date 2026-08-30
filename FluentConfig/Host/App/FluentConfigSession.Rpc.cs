using System;
using FluentConfig.Protocol;

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
                    case RpcMethods.FilepathValidate:
                        HandleFilepathValidate(msg);
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
    }
}
