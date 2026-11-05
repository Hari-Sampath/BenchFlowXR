using System;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>Maps visible breadboard holes to their electrical strip identifiers.</summary>
    public sealed class BreadboardGrid : MonoBehaviour
    {
        [Min(1)] public int rowCount = 30;
        [Min(1)] public int columnsPerSide = 5;
        [Min(0.001f)] public float holeSpacing = 0.045f;
        [Min(0.001f)] public float rowSpacing = 0.021f;
        public float centerGap = 0.08f;
        public float railOffset = 0.33f;

        public Vector3 HolePosition(HoleAddress hole)
        {
            float x;
            if (hole.rail != BreadboardRail.None)
                x = hole.rail == BreadboardRail.Positive ? -railOffset : railOffset;
            else
            {
                int sideColumn = Mathf.Clamp(hole.column, 0, columnsPerSide - 1);
                float half = (columnsPerSide - 1) * holeSpacing * 0.5f;
                x = hole.side == BreadboardSide.Left
                    ? -centerGap * 0.5f - half + sideColumn * holeSpacing
                    : centerGap * 0.5f + sideColumn * holeSpacing;
            }

            int row = Mathf.Clamp(hole.row, 0, rowCount - 1);
            return transform.TransformPoint(new Vector3(x, 0.012f, ((rowCount - 1) * 0.5f - row) * rowSpacing));
        }

        public bool TrySnap(Vector3 worldPosition, out HoleAddress hole, out Vector3 snappedPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            int rawRow = Mathf.RoundToInt((rowCount - 1) * 0.5f - local.z / rowSpacing);
            int row = rawRow;
            row = Mathf.Clamp(row, 0, rowCount - 1);

            float leftStart = -centerGap * 0.5f - (columnsPerSide - 1) * holeSpacing * 0.5f;
            float rightStart = centerGap * 0.5f;
            float railDistance = Mathf.Abs(local.x + railOffset);
            bool positiveRail = local.x < 0f;
            float negativeRailDistance = Mathf.Abs(local.x - railOffset);

            if (Mathf.Min(railDistance, negativeRailDistance) < holeSpacing * 0.55f)
            {
                hole = new HoleAddress(row, 0, BreadboardSide.Left,
                    positiveRail ? BreadboardRail.Positive : BreadboardRail.Ground);
            }
            else
            {
                BreadboardSide side = local.x < 0f ? BreadboardSide.Left : BreadboardSide.Right;
                float start = side == BreadboardSide.Left ? leftStart : rightStart;
                int column = Mathf.Clamp(Mathf.RoundToInt((local.x - start) / holeSpacing), 0, columnsPerSide - 1);
                hole = new HoleAddress(row, column, side, BreadboardRail.None);
            }

            snappedPosition = HolePosition(hole);
            float maxHalfLength = (rowCount - 1) * rowSpacing * 0.5f + rowSpacing * 0.5f;
            return rawRow >= 0 && rawRow < rowCount && Mathf.Abs(local.z) <= maxHalfLength &&
                   Mathf.Abs(local.x) <= railOffset + holeSpacing;
        }

        public string ElectricalNode(HoleAddress hole)
        {
            if (hole.rail != BreadboardRail.None) return "rail:" + hole.rail;
            return string.Format("strip:{0}:{1}", hole.side, hole.row);
        }

        public void ConnectBoardNets(CircuitGraph graph)
        {
            // Each five-hole group on a row is one strip. Rails run continuously end to end.
            graph.RegisterNode("rail:" + BreadboardRail.Positive);
            graph.RegisterNode("rail:" + BreadboardRail.Ground);
            for (int row = 0; row < rowCount; row++)
            {
                graph.RegisterNode(ElectricalNode(new HoleAddress(row, 0, BreadboardSide.Left, BreadboardRail.None)));
                graph.RegisterNode(ElectricalNode(new HoleAddress(row, 0, BreadboardSide.Right, BreadboardRail.None)));
            }
        }
    }

    public enum BreadboardSide { Left, Right }
    public enum BreadboardRail { None, Positive, Ground }

    [Serializable]
    public struct HoleAddress : IEquatable<HoleAddress>
    {
        public int row;
        public int column;
        public BreadboardSide side;
        public BreadboardRail rail;

        public HoleAddress(int row, int column, BreadboardSide side, BreadboardRail rail)
        { this.row = row; this.column = column; this.side = side; this.rail = rail; }

        public bool Equals(HoleAddress other) => row == other.row && column == other.column && side == other.side && rail == other.rail;
        public override bool Equals(object obj) => obj is HoleAddress other && Equals(other);
        public override int GetHashCode() => (row * 397) ^ (column * 31) ^ (int)side ^ ((int)rail << 8);
        public override string ToString() => rail == BreadboardRail.None ? $"{side}:{row}:{column}" : $"{rail}:{row}";
    }
}
