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
            if (rowCount < 1 || columnsPerSide < 1 || holeSpacing <= 0f || rowSpacing <= 0f)
            {
                hole = default;
                snappedPosition = worldPosition;
                return false;
            }

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            int rawRow = Mathf.RoundToInt((rowCount - 1) * 0.5f - local.z / rowSpacing);
            int row = Mathf.Clamp(rawRow, 0, Mathf.Max(0, rowCount - 1));
            float localZ = ((rowCount - 1) * 0.5f - row) * rowSpacing;

            float leftStart = -centerGap * 0.5f - (columnsPerSide - 1) * holeSpacing * 0.5f;
            float rightStart = centerGap * 0.5f;
            int leftColumn = Mathf.Clamp(Mathf.RoundToInt((local.x - leftStart) / holeSpacing), 0, columnsPerSide - 1);
            int rightColumn = Mathf.Clamp(Mathf.RoundToInt((local.x - rightStart) / holeSpacing), 0, columnsPerSide - 1);
            float leftDistance = Mathf.Abs(local.x - (leftStart + leftColumn * holeSpacing));
            float rightDistance = Mathf.Abs(local.x - (rightStart + rightColumn * holeSpacing));
            float positiveRailDistance = Mathf.Abs(local.x + railOffset);
            float groundRailDistance = Mathf.Abs(local.x - railOffset);
            float nearestDistance = Mathf.Min(Mathf.Min(leftDistance, rightDistance), Mathf.Min(positiveRailDistance, groundRailDistance));

            if (positiveRailDistance <= nearestDistance)
            {
                hole = new HoleAddress(row, 0, BreadboardSide.Left, BreadboardRail.Positive);
            }
            else if (groundRailDistance <= nearestDistance)
                hole = new HoleAddress(row, 0, BreadboardSide.Right, BreadboardRail.Ground);
            else if (leftDistance <= rightDistance)
                hole = new HoleAddress(row, leftColumn, BreadboardSide.Left, BreadboardRail.None);
            else
                hole = new HoleAddress(row, rightColumn, BreadboardSide.Right, BreadboardRail.None);

            snappedPosition = HolePosition(hole);
            float maxXDistance = holeSpacing * 0.5f + 0.00001f;
            float maxZDistance = rowSpacing * 0.5f + 0.00001f;
            return rowCount > 0 && columnsPerSide > 0 && holeSpacing > 0f && rowSpacing > 0f &&
                   rawRow >= 0 && rawRow < rowCount && Mathf.Abs(local.z - localZ) <= maxZDistance &&
                   nearestDistance <= maxXDistance;
        }

        public string ElectricalNode(HoleAddress hole)
        {
            if (hole.rail != BreadboardRail.None) return "rail:" + hole.rail;
            return string.Format("strip:{0}:{1}", hole.side, Mathf.Clamp(hole.row, 0, Mathf.Max(0, rowCount - 1)));
        }

        public bool TryGetElectricalNode(HoleAddress hole, out string node)
        {
            node = null;
            if (rowCount < 1 || columnsPerSide < 1 || hole.row < 0 || hole.row >= rowCount) return false;
            if (hole.rail != BreadboardRail.None)
            {
                if (hole.rail != BreadboardRail.Positive && hole.rail != BreadboardRail.Ground) return false;
                node = ElectricalNode(hole);
                return true;
            }

            if ((hole.side != BreadboardSide.Left && hole.side != BreadboardSide.Right) ||
                hole.column < 0 || hole.column >= columnsPerSide) return false;
            node = ElectricalNode(hole);
            return true;
        }

        public void ConnectBoardNets(CircuitGraph graph)
        {
            if (graph == null) return;
            // Each five-hole group on a row is one strip. Rails run continuously end to end.
            graph.RegisterNode("rail:" + BreadboardRail.Positive);
            graph.RegisterNode("rail:" + BreadboardRail.Ground);
            for (int row = 0; row < Mathf.Max(0, rowCount); row++)
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
