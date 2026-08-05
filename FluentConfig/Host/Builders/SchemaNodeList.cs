using System;
using System.Collections.Generic;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Collects schema nodes for a section or nested group/pill template.
    /// </summary>
    public sealed class SchemaNodeList
    {
        private readonly List<SchemaNode> _nodes = new List<SchemaNode>();
        private readonly HashSet<string> _saveKeys = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Reserves a saveKey for uniqueness within this list. Returns false if already used.</summary>
        public bool TryReserveSaveKey(string saveKey)
        {
            if (string.IsNullOrEmpty(saveKey)) return true;
            return _saveKeys.Add(saveKey);
        }

        public void Add(SchemaNode node)
        {
            if (node != null)
                _nodes.Add(node);
        }

        public List<SchemaNode> ToList() => new List<SchemaNode>(_nodes);
    }
}
