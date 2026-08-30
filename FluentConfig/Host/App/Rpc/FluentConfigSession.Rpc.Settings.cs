using FluentConfig.Core;
using FluentConfig.Protocol;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>Save and filepath RPC handlers.</summary>
    public sealed partial class FluentConfigSession
    {
        private void HandleSave(WireMessage msg)
        {
            var p = msg.Params?.ToObject<SaveParams>(ProtocolJson.CreateSerializer());
            var incoming = p?.Values ?? new JObject();
            var errors = FilepathValidation.ValidateAll(_filepathRules, incoming);
            if (errors.Count > 0)
            {
                _bridge?.Send(WireMessage.ResponseError(msg.Id.Value, "validation", string.Join(" ", errors)));
                return;
            }
            _latestValues = SettingsSync.ApplyAndSave(_settingsManager, incoming);
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new { ok = true }));
        }

        private void HandleFilepathBrowse(WireMessage msg)
        {
            var p = msg.Params?.ToObject<FilepathBrowseParams>(ProtocolJson.CreateSerializer());
            var rule = FilepathValidation.FindRule(_filepathRules, p?.SaveKey);
            var dlg = new OpenFileDialog();
            dlg.Filter = FilepathValidation.DialogFilter(rule?.Label, rule?.Accept);
            string path = null;
            if (dlg.ShowDialog(_window) == true)
                path = dlg.FileName;
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new FilepathBrowseResult { Path = path }));
        }

        private void HandleFilepathValidate(WireMessage msg)
        {
            var p = msg.Params?.ToObject<FilepathValidateParams>(ProtocolJson.CreateSerializer());
            var rule = FilepathValidation.FindRule(_filepathRules, p?.SaveKey);
            bool mustExist = rule == null || rule.MustExist;
            var accept = rule?.Accept;
            var error = FilepathValidation.Check(p?.Path, mustExist, accept);
            _bridge?.Send(WireMessage.ResponseResult(msg.Id.Value, new FilepathValidateResult
            {
                Ok = error == null,
                Error = error,
            }));
        }
    }
}
