using System;
using UnityEngine;

namespace BenchFlow
{
    public enum BreadboardSide { SideA, SideB } // SideA = rows a-e (front/lower), SideB = rows f-j (back/upper)
    public enum BreadboardRail { None, Positive, Ground }

    [Serializable]
    public struct HoleAddress : IEquatable<HoleAddress>
    {
        public int row; // 1 to rowCount (e.g. 1 to 63)
        public int column; // 0 to 4 (for SideA: 0=a, 1=b, 2=c, 3=d, 4=e. for SideB: 0=f, 1=g, 2=h, 3=i, 4=j)
        public BreadboardSide side;
        public BreadboardRail rail;

        public HoleAddress(int row, int column, BreadboardSide side, BreadboardRail rail = BreadboardRail.None)
        {
            this.row = row;
            this.column = column;
            this.side = side;
            this.rail = rail;
        }

        public string ColumnLetter
        {
            get
            {
                if (rail != BreadboardRail.None) return rail == BreadboardRail.Positive ? "+" : "-";
                return side == BreadboardSide.SideA
                    ? ("abcde"[Mathf.Clamp(column, 0, 4)]).ToString()
                    : ("fghij"[Mathf.Clamp(column, 0, 4)]).ToString();
            }
        }

        public bool Equals(HoleAddress other) =>
            row == other.row && column == other.column && side == other.side && rail == other.rail;

        public override bool Equals(object obj) => obj is HoleAddress other && Equals(other);
        public override int GetHashCode() => (row * 397) ^ (column * 31) ^ (int)side ^ ((int)rail << 8);
        public override string ToString() => rail == BreadboardRail.None ? $"{row}{ColumnLetter}" : $"{rail}:{row}";
    }

    /// <summary>
    /// Models the physical hole coordinates and electrical strips of the solderless breadboard.
    /// Rows 1 to 63 run horizontally along the X-axis (from left near controller to right).
    /// Columns a-e and f-j run along the Z-axis, separated by the center divider groove.
    /// </summary>
    public sealed class BreadboardGrid : MonoBehaviour
    {
        [Header("Grid Geometry")]
        [Min(1)] public int rowCount = 63;
        [Min(1)] public int columnsPerSide = 5;
        [Min(0.001f)] public float holeSpacing = 0.00254f; // 2.54 mm standard pitch
        [Min(0.001f)] public float rowSpacing = 0.00254f;  // 2.54 mm standard pitch
        public float centerGap = 0.00762f;                // 7.62 mm center divider
        public float railOffset = 0.0215f;                // Power rail offset along Z
        public float surfaceY = 0.0135f;                  // Height of breadboard top surface

        public static BreadboardGrid Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        /// <summary>Returns world space position of a given hole address.</summary>
        public Vector3 HolePosition(HoleAddress hole)
        {
            return transform.TransformPoint(HoleLocalPosition(hole));
        }

        /// <summary>Returns local space position of a given hole address.</summary>
        public Vector3 HoleLocalPosition(HoleAddress hole)
        {
            // Center row is at X = 0. Row 1 is to the left (-X), Row 63 is to the right (+X)
            float centerRow = (rowCount + 1) * 0.5f;
            float x = (hole.row - centerRow) * rowSpacing;

            float z;
            if (hole.rail != BreadboardRail.None)
            {
                z = hole.rail == BreadboardRail.Positive ? -railOffset - holeSpacing : -railOffset;
            }
            else
            {
                int col = Mathf.Clamp(hole.column, 0, columnsPerSide - 1);
                if (hole.side == BreadboardSide.SideA)
                {
                    // Columns a, b, c, d, e (towards -Z / front): e is nearest center, a is nearest rail
                    z = -centerGap * 0.5f - (4 - col) * holeSpacing;
                }
                else
                {
                    // Columns f, g, h, i, j (towards +Z / back): f is nearest center, j is nearest upper rail
                    z = centerGap * 0.5f + col * holeSpacing;
                }
            }

            return new Vector3(x, surfaceY, z);
        }

        /// <summary>Parse a string like '14c', '16c', '8d', '16a' into HoleAddress.</summary>
        public bool TryParseHole(string text, out HoleAddress hole)
        {
            hole = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim().ToLowerInvariant();
            char lastChar = text[text.Length - 1];
            string rowStr = text.Substring(0, text.Length - 1);

            if (!int.TryParse(rowStr, out int row) || row < 1 || row > rowCount) return false;

            int sideAIdx = "abcde".IndexOf(lastChar);
            if (sideAIdx >= 0)
            {
                hole = new HoleAddress(row, sideAIdx, BreadboardSide.SideA, BreadboardRail.None);
                return true;
            }

            int sideBIdx = "fghij".IndexOf(lastChar);
            if (sideBIdx >= 0)
            {
                hole = new HoleAddress(row, sideBIdx, BreadboardSide.SideB, BreadboardRail.None);
                return true;
            }

            if (lastChar == '+' || text.EndsWith("vcc"))
            {
                hole = new HoleAddress(row, 0, BreadboardSide.SideA, BreadboardRail.Positive);
                return true;
            }
            if (lastChar == '-' || text.EndsWith("gnd"))
            {
                hole = new HoleAddress(row, 0, BreadboardSide.SideA, BreadboardRail.Ground);
                return true;
            }

            return false;
        }

        public HoleAddress GetHole(string text)
        {
            if (TryParseHole(text, out HoleAddress hole)) return hole;
            throw new ArgumentException($"Invalid breadboard hole identifier: '{text}'");
        }

        /// <summary>Snap a world position to the nearest breadboard hole.</summary>
        public bool TrySnap(Vector3 worldPosition, out HoleAddress hole, out Vector3 snappedWorldPosition, float maxDistance = 0.012f)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);

            // Calculate nearest row
            float centerRow = (rowCount + 1) * 0.5f;
            int row = Mathf.Clamp(Mathf.RoundToInt(centerRow + local.x / rowSpacing), 1, rowCount);

            // Determine if closer to rails or terminal strips
            float absZ = Mathf.Abs(local.z);
            float distToBottomRail = Mathf.Abs(local.z - (-railOffset));
            float distToTopRail = Mathf.Abs(local.z - railOffset);

            if (distToBottomRail < holeSpacing * 0.9f)
            {
                hole = new HoleAddress(row, 0, BreadboardSide.SideA, BreadboardRail.Ground);
            }
            else if (distToTopRail < holeSpacing * 0.9f)
            {
                hole = new HoleAddress(row, 0, BreadboardSide.SideB, BreadboardRail.Positive);
            }
            else
            {
                BreadboardSide side = local.z < 0f ? BreadboardSide.SideA : BreadboardSide.SideB;
                int col;
                if (side == BreadboardSide.SideA)
                {
                    // z = -centerGap * 0.5f - (4 - col) * holeSpacing
                    float targetDist = -local.z - centerGap * 0.5f;
                    col = 4 - Mathf.Clamp(Mathf.RoundToInt(targetDist / holeSpacing), 0, columnsPerSide - 1);
                }
                else
                {
                    // z = centerGap * 0.5f + col * holeSpacing
                    float targetDist = local.z - centerGap * 0.5f;
                    col = Mathf.Clamp(Mathf.RoundToInt(targetDist / holeSpacing), 0, columnsPerSide - 1);
                }
                hole = new HoleAddress(row, col, side, BreadboardRail.None);
            }

            snappedWorldPosition = HolePosition(hole);
            float dist = Vector3.Distance(worldPosition, snappedWorldPosition);
            return dist <= maxDistance;
        }

        /// <summary>Electrical strip identifier. Holes on the same 5-hole strip share the same ID.</summary>
        public string ElectricalNode(HoleAddress hole)
        {
            if (hole.rail == BreadboardRail.Ground) return "rail:GND";
            if (hole.rail == BreadboardRail.Positive) return "rail:VCC";
            return $"strip:{(hole.side == BreadboardSide.SideA ? 'A' : 'B')}:{hole.row}";
        }

        /// <summary>Registers all terminal strips and rails into the circuit netlist.</summary>
        public void ConnectBoardNets(CircuitGraph graph)
        {
            graph.RegisterNode("rail:VCC");
            graph.RegisterNode("rail:GND");
            for (int r = 1; r <= rowCount; r++)
            {
                graph.RegisterNode($"strip:A:{r}");
                graph.RegisterNode($"strip:B:{r}");
            }
        }
    }
}
