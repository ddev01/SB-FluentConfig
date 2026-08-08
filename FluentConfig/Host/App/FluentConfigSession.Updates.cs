using System;
using System.Threading;
using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Updater;

namespace FluentConfig
{
    /// <summary>Deferred GitHub update-check + <c>update.available</c> push; version gates.</summary>
    public sealed partial class FluentConfigSession
    {
        internal void OnWebReady()
        {
            BeginDeferredUpdateCheck();
        }

        /// <summary>
        /// Optional min Streamer.bot / FluentConfig gates for extension notices.
        /// Called before the window opens; failures show a MessageBox and abort Show.
        /// </summary>
        private bool TryValidateExtensionVersionGates(out string message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(_updateRepo))
                return true;

            if (!string.IsNullOrWhiteSpace(_updateMinStreamerBot))
            {
                string sbVersion = null;
                try { sbVersion = _cph?.GetVersion(); }
                catch { /* best-effort */ }

                if (string.IsNullOrWhiteSpace(sbVersion)
                    || GitHubUpdater.IsNewer(_updateMinStreamerBot, sbVersion))
                {
                    message =
                        "This extension requires Streamer.bot version "
                        + _updateMinStreamerBot
                        + " or higher. You currently are on version "
                        + (string.IsNullOrWhiteSpace(sbVersion) ? "(unknown)" : sbVersion)
                        + ". Please update Streamer.bot.";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_updateMinFluentConfig))
            {
                var fcVersion = FrameworkVersion;
                if (string.IsNullOrWhiteSpace(fcVersion)
                    || GitHubUpdater.IsNewer(_updateMinFluentConfig, fcVersion))
                {
                    message =
                        "This extension requires FluentConfig.dll version "
                        + _updateMinFluentConfig
                        + " or higher. You currently are on version "
                        + (string.IsNullOrWhiteSpace(fcVersion) ? "(unknown)" : fcVersion)
                        + ". Please update FluentConfig.dll (run the DllCheck action).";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Run configured extension update HTTP off the Show critical path after bootstrap is sent.
        /// Daily throttle + ignored-version prefs; pushes <c>update.available</c> (notify-only).
        /// </summary>
        private void BeginDeferredUpdateCheck()
        {
            if (string.IsNullOrWhiteSpace(_updateRepo))
                return;
            if (Interlocked.Exchange(ref _updateCheckStarted, 1) != 0)
                return;

            var prefs = UpdatePrefsStore.Load(_cph, _title);
            if (UpdatePrefsStore.IsWithinDailyThrottle(prefs))
                return;

            var repo = _updateRepo;
            var currentVersion = _updateCurrentVersion;
            var tagPrefix = _updateTagPrefix;
            var guideUrl = _updateGuideUrl;
            var ignoredSnapshot = prefs?.IgnoredVersion;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    UpdateCheckResult result = string.IsNullOrEmpty(tagPrefix)
                        ? GitHubUpdater.CheckForLatestRelease(repo, currentVersion)
                        : GitHubUpdater.CheckForTaggedRelease(repo, tagPrefix, currentVersion);

                    // Record the attempt even when no update / network failure so we don't hammer GitHub.
                    UpdatePrefsStore.MarkChecked(_cph, _title);

                    if (result == null || !result.UpdateAvailable)
                        return;

                    var ignoredPrefs = new UpdatePrefsData { IgnoredVersion = ignoredSnapshot };
                    // Re-load in case IgnoreVersion raced; prefer fresh prefs when available.
                    var fresh = UpdatePrefsStore.Load(_cph, _title);
                    if (fresh != null)
                        ignoredPrefs = fresh;

                    if (UpdatePrefsStore.IsVersionIgnored(ignoredPrefs, result.LatestVersion))
                        return;

                    var dispatcher = _window?.Dispatcher;
                    if (dispatcher == null)
                        return;

                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_bridge == null)
                            return;
                        _pendingUpdate = result;
                        PushUpdateAvailableNotice(guideUrl);
                    }));
                }
                catch (Exception ex)
                {
                    Log("[FluentConfig] deferred update check failed: " + ex.Message);
                }
            });
        }

        private void PushUpdateAvailableNotice(string guideUrl)
        {
            if (_pendingUpdate == null || !_pendingUpdate.UpdateAvailable)
                return;

            _bridge?.Send(WireMessage.Push(PushEventNames.UpdateAvailable, new UpdateAvailablePayload
            {
                NoticeId = "extension-update",
                CurrentVersion = _pendingUpdate.CurrentVersion,
                LatestVersion = _pendingUpdate.LatestVersion,
                ReleaseNotes = _pendingUpdate.ReleaseNotes,
                DownloadUrl = null,
                Repo = _updateRepo,
                Mode = "notify",
                ReleasePageUrl = _pendingUpdate.ReleasePageUrl,
                UpdateGuideUrl = string.IsNullOrWhiteSpace(guideUrl)
                    ? _pendingUpdate.ReleasePageUrl
                    : guideUrl,
            }));
        }

        /// <summary>Persist dismiss reason from <c>update.dismiss</c> RPC.</summary>
        internal void HandleUpdateDismiss(UpdateDismissParams p)
        {
            if (p == null)
                return;

            var reason = (p.Reason ?? "later").Trim();
            if (string.Equals(reason, "ignoreVersion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(reason, "ignore-version", StringComparison.OrdinalIgnoreCase))
            {
                var version = !string.IsNullOrWhiteSpace(p.Version)
                    ? p.Version
                    : _pendingUpdate?.LatestVersion;
                if (!string.IsNullOrWhiteSpace(version))
                    UpdatePrefsStore.IgnoreVersion(_cph, _title, version);
            }
            // "later" — close modal only; daily throttle handles re-ask.
        }
    }
}
