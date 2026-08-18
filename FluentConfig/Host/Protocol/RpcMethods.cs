using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Protocol
{
    /// <summary>Well-known RPC method names (either direction). Params/result shapes below.</summary>
    public static class RpcMethods
    {
        // --- Web → Host ---

        /// <summary>Persist the current values blob. Params: <see cref="SaveParams"/>.</summary>
        public const string Save = "save";

        /// <summary>Reload dropdown options. Params: <see cref="DropdownRefreshParams"/>. Result: <see cref="DropdownRefreshResult"/>.</summary>
        public const string DropdownRefresh = "dropdown.refresh";

        /// <summary>User clicked a button. Params: <see cref="ButtonClickParams"/>.</summary>
        public const string ButtonClick = "button.click";

        /// <summary>Open a native file picker. Params: <see cref="FilepathBrowseParams"/>. Result: <see cref="FilepathBrowseResult"/>.</summary>
        public const string FilepathBrowse = "filepath.browse";

        /// <summary>Check a filepath value (exists + optional extension). Params: <see cref="FilepathValidateParams"/>. Result: <see cref="FilepathValidateResult"/>.</summary>
        public const string FilepathValidate = "filepath.validate";

        /// <summary>Pill list changed (add/remove/rename). Params: <see cref="PillChangedParams"/>. Result may include updated item schemas.</summary>
        public const string PillChanged = "pill.changed";

        /// <summary>Stage a DLL update (test/tooling). Params: <see cref="UpdateStageParams"/>.</summary>
        public const string UpdateStage = "update.stage";

        /// <summary>
        /// User dismissed the extension update modal.
        /// Params: <see cref="UpdateDismissParams"/> (<c>reason</c>: <c>later</c> | <c>ignoreVersion</c>).
        /// </summary>
        public const string UpdateDismiss = "update.dismiss";

        /// <summary>
        /// In-app Exit button asks the host to close. Params: <see cref="WindowCloseParams"/>.
        /// When <c>alreadyConfirmed</c> is true, host skips the <see cref="WindowCloseRequested"/> round-trip.
        /// </summary>
        public const string WindowClose = "window.close";

        /// <summary>
        /// Open an external URL in the system browser. Params: <see cref="ShellOpenUrlParams"/>.
        /// Fire-and-forget from the web (footer, update-notice links, rich-text links).
        /// </summary>
        public const string ShellOpenUrl = "shell.openUrl";

        /// <summary>
        /// Web → host performance mark. Params: <see cref="PerfMarkParams"/>.
        /// Meaningful only when compiled with FC_PERF_TRACE. Arbitrary <c>name</c> values are recorded as
        /// milestones; only <c>web-ready</c> closes the summary and exports <c>FluentConfig_PerfLast</c>.
        /// </summary>
        public const string PerfMark = "perf.mark";

        // --- Host → Web (UI affordances formerly on UiContext) ---

        /// <summary>Show a confirm dialog in the web UI. Params: <see cref="ConfirmParams"/>. Result: <see cref="ConfirmResult"/>.</summary>
        public const string DialogConfirm = "dialog.confirm";

        /// <summary>Show a modal popup. Params: <see cref="PopupParams"/>.</summary>
        public const string DialogPopup = "dialog.popup";

        /// <summary>Show a toast. Params: <see cref="ToastParams"/>.</summary>
        public const string Toast = "toast.show";

        /// <summary>
        /// Native title-bar close: host asks web whether discard is allowed.
        /// Result: <see cref="WindowCloseRequestedResult"/>.
        /// </summary>
        public const string WindowCloseRequested = "window.closeRequested";
    }

    public sealed class SaveParams
    {
        [JsonProperty("values")]
        public JObject Values { get; set; }
    }

    public sealed class DropdownRefreshParams
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }
    }

    public sealed class DropdownRefreshResult
    {
        [JsonProperty("options")]
        public IList<DropdownOption> Options { get; set; }
    }

    public sealed class ButtonClickParams
    {
        [JsonProperty("buttonId")]
        public string ButtonId { get; set; }

        /// <summary>Latest values snapshot so host OnClick can read Pending-equivalent state.</summary>
        [JsonProperty("values")]
        public JObject Values { get; set; }
    }

    public sealed class FilepathBrowseParams
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }
    }

    public sealed class FilepathBrowseResult
    {
        /// <summary>Selected path, or null/omitted if cancelled.</summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }

    public sealed class FilepathValidateParams
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }

    public sealed class FilepathValidateResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    public sealed class PillChangedParams
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        /// <summary>"add" | "remove"</summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Full current pill name list after the change.</summary>
        [JsonProperty("items")]
        public IList<string> Items { get; set; }
    }

    public sealed class PillChangedResult
    {
        /// <summary>Host-built nested schema for the affected pill(s), if any.</summary>
        [JsonProperty("items")]
        public IList<PillItemSchema> Items { get; set; }

        /// <summary>Settings keys the host removed (e.g. on pill remove).</summary>
        [JsonProperty("removedKeys")]
        public IList<string> RemovedKeys { get; set; }
    }

    public sealed class UpdateStageParams
    {
        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("noticeId")]
        public string NoticeId { get; set; }
    }

    public sealed class UpdateDismissParams
    {
        [JsonProperty("noticeId")]
        public string NoticeId { get; set; }

        /// <summary><c>later</c> (default) or <c>ignoreVersion</c>.</summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>Version to ignore when <see cref="Reason"/> is <c>ignoreVersion</c>.</summary>
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public sealed class ConfirmParams
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("confirmText")]
        public string ConfirmText { get; set; }

        [JsonProperty("cancelText")]
        public string CancelText { get; set; }
    }

    public sealed class ConfirmResult
    {
        [JsonProperty("confirmed")]
        public bool Confirmed { get; set; }
    }

    public sealed class PopupParams
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public sealed class ToastParams
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public sealed class WindowCloseParams
    {
        /// <summary>
        /// True when the web already ran the dirty-check/dialog (in-app Exit path).
        /// </summary>
        [JsonProperty("alreadyConfirmed")]
        public bool AlreadyConfirmed { get; set; }

        /// <summary>
        /// When true (Exit path), persist per-title "don't remind before discard" preference.
        /// </summary>
        [JsonProperty("dontRemindAgain")]
        public bool DontRemindAgain { get; set; }
    }

    public sealed class WindowCloseRequestedResult
    {
        [JsonProperty("allowClose")]
        public bool AllowClose { get; set; }

        [JsonProperty("dontRemindAgain")]
        public bool DontRemindAgain { get; set; }
    }

    public sealed class ShellOpenUrlParams
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public sealed class PerfMarkParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
