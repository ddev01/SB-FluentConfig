using System;
using System.Threading;
using FluentConfig.Protocol;
using FluentConfig.Updater;

namespace FluentConfig
{
    /// <summary>Deferred GitHub update-check + <c>update.available</c> push.</summary>
    public sealed partial class FluentConfigSession
    {
        internal void OnWebReady()
        {
            BeginDeferredUpdateCheck();
        }

        /// <summary>
        /// Run configured update HTTP off the Show critical path after bootstrap is sent.
        /// Pushes <c>update.available</c> on the UI thread when a newer release exists.
        /// </summary>
        private void BeginDeferredUpdateCheck()
        {
            if (string.IsNullOrWhiteSpace(_updateRepo))
                return;
            if (Interlocked.Exchange(ref _updateCheckStarted, 1) != 0)
                return;

            var repo = _updateRepo;
            var currentVersion = _updateCurrentVersion;
            var tagPrefix = _updateTagPrefix;
            var mode = _updateMode ?? "self";
            var isNotify = string.Equals(mode, "notify", StringComparison.Ordinal);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    UpdateCheckResult result = isNotify
                        ? GitHubUpdater.CheckForTaggedRelease(repo, tagPrefix, currentVersion)
                        : GitHubUpdater.CheckForUpdate(repo, currentVersion);

                    if (result == null || !result.UpdateAvailable)
                        return;

                    var dispatcher = _window?.Dispatcher;
                    if (dispatcher == null)
                        return;

                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_bridge == null)
                            return;
                        _pendingUpdate = result;
                        PushUpdateAvailableNotice();
                    }));
                }
                catch (Exception ex)
                {
                    Log("[FluentConfig] deferred update check failed: " + ex.Message);
                }
            });
        }

        private void PushUpdateAvailableNotice()
        {
            if (_pendingUpdate == null || !_pendingUpdate.UpdateAvailable)
                return;

            var isNotify = string.Equals(_updateMode, "notify", StringComparison.Ordinal);
            _bridge?.Send(WireMessage.Push(PushEventNames.UpdateAvailable, new UpdateAvailablePayload
            {
                NoticeId = isNotify ? "extension-update" : "self-update",
                CurrentVersion = _pendingUpdate.CurrentVersion,
                LatestVersion = _pendingUpdate.LatestVersion,
                ReleaseNotes = _pendingUpdate.ReleaseNotes,
                DownloadUrl = _pendingUpdate.DownloadUrl,
                Repo = _updateRepo,
                Mode = _updateMode ?? "self",
                ReleasePageUrl = _pendingUpdate.ReleasePageUrl,
            }));
        }
    }
}
