using System;
using System.Collections.Generic;

namespace FluentConfig
{
    /// <summary>Per-document generated schema ids (grid_*, vis_*, btn_*) so schema.patch can resolve uniquely.</summary>
    public sealed partial class FluentConfigSession
    {
        private readonly HashSet<string> _generatedIds =
            new HashSet<string>(StringComparer.Ordinal);

        internal string UniqueGeneratedId(string baseId)
        {
            if (string.IsNullOrEmpty(baseId))
                baseId = "id";
            if (_generatedIds.Add(baseId))
                return baseId;
            for (var n = 2; ; n++)
            {
                var candidate = baseId + "_" + n;
                if (_generatedIds.Add(candidate))
                    return candidate;
            }
        }
    }
}
