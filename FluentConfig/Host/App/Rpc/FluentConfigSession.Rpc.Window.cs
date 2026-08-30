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
            var alreadyConfirmed = p != null && p.AlreadyConfirmed;
            var dontRemindAgain = p != null && p.DontRemindAgain;

            if (alreadyConfirmed)
            {
                _closeAlreadyConfirmed = true;
                if (dontRemindAgain)
                {
                    _dontRemindDiscard = true;
                    WindowPrefsStore.SetDontRemindDiscard(_cph, ScopeKey, true);
                }

                _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
                // WebMessageReceived already runs on the UI thread.
                _window?.CloseAllowed();
                return;
            }

            // alreadyConfirmed: false — same ask-the-web path as native chrome close.
            // Do not persist dontRemind from this RPC; the closeRequested reply carries it.
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value));
            OnNativeCloseRequested();
        }
    }
}
