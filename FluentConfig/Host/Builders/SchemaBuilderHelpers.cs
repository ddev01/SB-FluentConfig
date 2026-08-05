using System;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Shared schema helpers used by <see cref="SectionBuilder"/> and <see cref="PanelBuilder"/>.
    /// </summary>
    internal static class SchemaBuilderHelpers
    {
        public static void AddVisibilityGroup(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            string toggleKey,
            Action<PanelBuilder> build,
            bool inverted)
        {
            flushPending();
            if (build == null) return;

            var list = new SchemaNodeList();
            var pb = new PanelBuilder(session, list);
            build(pb);
            pb.FlushPending();

            nodes.Add(new GroupNode
            {
                Id = "vis_" + (toggleKey ?? "group"),
                Visibility = new VisibilityCondition
                {
                    SaveKey = toggleKey,
                    EqualsValue = true,
                    Inverted = inverted,
                },
                Children = list.ToList(),
            });
        }

        public static void AddRepeatableRows(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            string saveKey,
            Action<PanelBuilder> buildRow)
        {
            flushPending();
            if (buildRow == null) return;

            var list = new SchemaNodeList();
            var pb = new PanelBuilder(session, list);
            buildRow(pb);
            pb.FlushPending();

            nodes.Add(new RepeatableRowsNode
            {
                Id = saveKey,
                SaveKey = saveKey,
                RowSchema = list.ToList(),
            });
        }
    }
}
