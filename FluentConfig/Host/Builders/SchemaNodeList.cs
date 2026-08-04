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

        public IReadOnlyList<SchemaNode> Nodes => _nodes;

        public void Add(SchemaNode node)
        {
            if (node != null)
                _nodes.Add(node);
        }

        public List<SchemaNode> ToList() => new List<SchemaNode>(_nodes);
    }
}
