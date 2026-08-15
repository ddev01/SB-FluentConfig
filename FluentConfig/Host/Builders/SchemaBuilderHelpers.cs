using System;
using System.Collections.Generic;
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
            AddVisibilityGroup(
                session,
                nodes,
                flushPending,
                new VisibilityCondition
                {
                    SaveKey = toggleKey,
                    EqualsValue = true,
                    Inverted = inverted,
                },
                "vis_" + (toggleKey ?? "group"),
                build,
                indented: null);
        }

        public static void AddVisibilityGroup(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            VisibilityCondition visibility,
            string groupId,
            Action<PanelBuilder> build,
            bool? indented = false)
        {
            flushPending();
            if (build == null)
                return;
            var list = new SchemaNodeList();
            var pb = new PanelBuilder(session, list);
            build(pb);
            pb.FlushPending();
            nodes.Add(new GroupNode
            {
                Id = groupId ?? "vis_group",
                Visibility = visibility,
                Children = list.ToList(),
                Indented = indented,
            });
        }

        internal static bool? IndentedFromChrome(VisibilityChrome chrome) =>
            chrome == VisibilityChrome.Indented ? true : (bool?)false;

        public static void AddEqualsVisibilityGroup(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            string key,
            object equalsValue,
            bool inverted,
            Action<PanelBuilder> build,
            VisibilityChrome chrome)
        {
            var token = equalsValue?.ToString() ?? "null";
            AddVisibilityGroup(
                session,
                nodes,
                flushPending,
                new VisibilityCondition
                {
                    SaveKey = key,
                    EqualsValue = equalsValue,
                    Inverted = inverted,
                },
                "vis_" + (key ?? "group") + "_" + token,
                build,
                IndentedFromChrome(chrome));
        }

        public static void AddRepeatableRows(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            string saveKey,
            Action<PanelBuilder> buildRow)
        {
            flushPending();
            if (buildRow == null)
                return;
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

        public static void AddLayoutGroup(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            GridSpec spec,
            Action<PanelBuilder> build)
        {
            flushPending();
            if (build == null)
                return;
            var list = new SchemaNodeList();
            list.ContainerMode = spec?.Mode;
            var pb = new PanelBuilder(session, list);
            build(pb);
            pb.FlushPending();
            var mode = spec?.Mode ?? "grid";
            var id = mode == "row"
                ? "row_" + (spec?.Gap ?? 3)
                : "grid_" + (spec?.Columns?.ToString() ?? "auto");
            nodes.Add(new GroupNode
            {
                Id = id,
                Grid = spec,
                Children = list.ToList(),
            });
        }

        public static void AddRepeatFor(
            FluentConfigSession session,
            SchemaNodeList nodes,
            Action flushPending,
            string driverKey,
            Action<PanelBuilder, int> build,
            int? max,
            int startIndex = 1)
        {
            flushPending();
            if (build == null)
                return;
            if (string.IsNullOrWhiteSpace(driverKey))
                throw new ArgumentException("driverKey is required (non-null, non-empty).", nameof(driverKey));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be >= 0.");

            double declaredMin = 0;
            double declaredMax;
            if (max.HasValue)
            {
                declaredMax = max.Value;
                if (session != null && session.TryGetDeclaredRange(driverKey, out var min, out _))
                    declaredMin = min;
            }
            else
            {
                if (session == null || !session.TryGetDeclaredRange(driverKey, out declaredMin, out declaredMax))
                {
                    throw new InvalidOperationException(
                        $"RepeatFor(\"{driverKey}\"): max was omitted and the driver has no declared .Range(...) yet. " +
                        "Declare the driver control with .Range(min, max) in an earlier section (or earlier in this section), " +
                        "or pass an explicit max.");
                }
            }

            int resolvedMax = (int)Math.Round(declaredMax);
            int resolvedMin = (int)Math.Round(declaredMin);
            if (resolvedMax < startIndex)
                return;

            for (int i = startIndex; i <= resolvedMax; i++)
            {
                var list = new SchemaNodeList();
                var pb = new PanelBuilder(session, list);
                build(pb, i);
                pb.FlushPending();
                var children = list.ToList();
                if (children.Count == 0)
                    continue;

                if (i <= resolvedMin)
                {
                    foreach (var child in children)
                        nodes.Add(child);
                }
                else
                {
                    nodes.Add(new GroupNode
                    {
                        Id = "repeat_" + driverKey + "_" + i,
                        Visibility = new VisibilityCondition
                        {
                            SaveKey = driverKey,
                            EqualsValue = null,
                            Operator = "gte",
                            Value = i,
                            Inverted = false,
                        },
                        Children = children,
                        Indented = false,
                    });
                }
            }
        }
    }
}
