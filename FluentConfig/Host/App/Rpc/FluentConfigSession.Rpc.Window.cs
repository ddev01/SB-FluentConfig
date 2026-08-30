using FluentConfig.Core;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>In-app window.close RPC handler.</summary>
    public sealed partial class FluentConfigSession
    {
        private void HandleWindowClose(WireMessage msg)
        {
            var p = msg.Params?.ToObject<WindowCloseParams>(ProtocolJson.CreateSerializer());
            if (p != null && p.AlreadyConfirmed)
                _closeAlreadyConfirmed = true;
            if (p != null && p.DontRemindAgain)
            {
                _dontRemindDiscard = true;
                WindowPrefsStore.SetDontRemindDiscard(_cph, ScopeKey, true);
            }

            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));

            // WebMessageReceived already runs on the UI thread.
            _window?.CloseAllowed();
        }
    }
}
