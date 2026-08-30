using System;
using FluentConfig.Protocol;
using FluentConfig.Updater;

namespace FluentConfig
{
    /// <summary>update.stage and update.dismiss RPC handlers.</summary>
    public sealed partial class FluentConfigSession
    {
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
    }
}
