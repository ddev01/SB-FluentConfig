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

        /// <summary>Pill list changed (add/remove/rename). Params: <see cref="PillChangedParams"/>. Result may include updated item schemas.</summary>
        public const string PillChanged = "pill.changed";

        /// <summary>User accepted the update-notice banner. Params: <see cref="UpdateStageParams"/>.</summary>
        public const string UpdateStage = "update.stage";

        /// <summary>User dismissed the update-notice banner. Params: <see cref="UpdateDismissParams"/>.</summary>
        public const string UpdateDismiss = "update.dismiss";

        /// <summary>Forward a log line to the host log callback. Params: <see cref="LogParams"/>.</summary>
        public const string Log = "log";

        // --- Host → Web (UI affordances formerly on UiContext) ---

        /// <summary>Show a confirm dialog in the web UI. Params: <see cref="ConfirmParams"/>. Result: <see cref="ConfirmResult"/>.</summary>
        public const string DialogConfirm = "dialog.confirm";

        /// <summary>Show a modal popup. Params: <see cref="PopupParams"/>.</summary>
        public const string DialogPopup = "dialog.popup";

        /// <summary>Show a toast. Params: <see cref="ToastParams"/>.</summary>
        public const string Toast = "toast.show";
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

    public sealed class PillChangedParams
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        /// <summary>"add" | "remove" | "rename"</summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("previousName")]
        public string PreviousName { get; set; }

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
    }

    public sealed class LogParams
    {
        [JsonProperty("message")]
        public string Message { get; set; }
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
}
