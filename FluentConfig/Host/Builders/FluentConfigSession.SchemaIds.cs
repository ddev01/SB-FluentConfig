using System;
using System.Collections.Generic;

namespace FluentConfig
{
    /// <summary>Per-document generated schema ids (grid_*, vis_*, btn_*) so schema.patch can resolve uniquely.</summary>
    public sealed partial class FluentConfigSession
    {
        private readonly Dictionary<string, int> _generatedIdCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);

        internal string UniqueGeneratedId(string baseId)
        {
            if (string.IsNullOrEmpty(baseId))
                baseId = "id";
            if (!_generatedIdCounts.TryGetValue(baseId, out var n))
            {
                _generatedIdCounts[baseId] = 1;
                return baseId;
            }

            n++;
            _generatedIdCounts[baseId] = n;
            return baseId + "_" + n;
        }
    }
}
