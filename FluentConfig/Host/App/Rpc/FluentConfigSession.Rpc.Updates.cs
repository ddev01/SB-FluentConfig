using System;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>update.dismiss RPC handler.</summary>
    public sealed partial class FluentConfigSession
    {
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
