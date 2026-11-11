using System;
using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Package-independent hand interaction bridge. Bind the XR SDK's grab, release, rotate,
    /// and select events to these public methods; the hand transform supplies the tracked pose.
    /// </summary>
    public sealed class WorkbenchXRInteractor : MonoBehaviour
    {
        [Serializable]
        public sealed class FixedWireEndpoint
        {
            public string nodeId;
            public Transform endpoint;
        }

        public Transform handPoseSource;
        public Transform rayOrigin;
        public WorkbenchState workbench;
        public float rayDistance = 1.5f;
        public LayerMask interactionLayers = ~0;
        public FixedWireEndpoint[] fixedWireEndpoints;
        public Material wireMaterial;
        [Min(0.001f)] public float wireWidth = 0.003f;
        public float wireArchHeight = 0.025f;

        CircuitPart heldPart;
        Vector3 handLocalPosition;
        Quaternion handLocalRotation;
        bool wiring;
        WireEndpoint pendingEndpoint;
        LineRenderer pendingWirePreview;
        readonly List<WireSegment> wireVisuals = new List<WireSegment>();

        struct WireEndpoint
        {
            public string node;
            public Transform anchor;
            public Vector3 localPosition;

            public Vector3 Position => anchor != null ? anchor.TransformPoint(localPosition) : Vector3.zero;
        }

        sealed class WireSegment
        {
            public WireEndpoint from;
            public WireEndpoint to;
            public LineRenderer line;
        }

        void Update()
        {
            if (heldPart != null) UpdateHeldPart();
            UpdateWireVisuals();
            UpdatePendingWirePreview();
        }

        /// <summary>Bind to the XR SDK's select/grab started event.</summary>
        public void Grab()
        {
            if (heldPart != null || handPoseSource == null || workbench == null) return;
            Transform origin = rayOrigin != null ? rayOrigin : handPoseSource;
            if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, rayDistance, interactionLayers)) return;
            CircuitPart part = hit.collider.GetComponentInParent<CircuitPart>();
            if (part == null) return;

            heldPart = part;
            Quaternion inverseHandRotation = Quaternion.Inverse(handPoseSource.rotation);
            handLocalPosition = inverseHandRotation * (part.transform.position - handPoseSource.position);
            handLocalRotation = inverseHandRotation * part.transform.rotation;
            workbench.BeginGrab(part, HandPose);
        }

        /// <summary>Bind to the XR SDK's select/grab ended event.</summary>
        public void Release()
        {
            if (heldPart == null || workbench == null) return;
            CircuitPart released = heldPart;
            heldPart = null;
            workbench.EndGrab(released, released.transform.position);
        }

        /// <summary>Bind positive or negative angle values to a thumbstick or rotate button.</summary>
        public void RotateHeld(float degrees)
        {
            if (heldPart == null) return;
            handLocalRotation = Quaternion.AngleAxis(degrees, Vector3.up) * handLocalRotation;
            UpdateHeldPart();
        }

        /// <summary>Toggle endpoint selection; select two endpoints to add a circuit wire.</summary>
        public void ToggleWireMode()
        {
            wiring = !wiring;
            CancelWireSelection();
        }

        /// <summary>Bind to the XR SDK's select event while wire mode is active.</summary>
        public void SelectWireEndpoint()
        {
            if (!wiring || handPoseSource == null || workbench == null) return;
            Transform origin = rayOrigin != null ? rayOrigin : handPoseSource;
            bool gotHit = Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, rayDistance, interactionLayers);
            WireEndpoint endpoint;
            bool foundEndpoint = gotHit && TryGetEndpoint(hit, out endpoint);
            if (!foundEndpoint && !TryFindFixedEndpoint(origin, out endpoint)) return;
            if (string.IsNullOrEmpty(endpoint.node)) return;
            if (pendingEndpoint.node == null)
            {
                pendingEndpoint = endpoint;
                pendingWirePreview = CreateWireLine("Workbench Wire Preview");
                return;
            }

            if (pendingEndpoint.node != endpoint.node)
            {
                workbench.ConnectWire(pendingEndpoint.node, endpoint.node);
                DrawWire(pendingEndpoint, endpoint);
            }
            CancelWireSelection();
        }

        public void CancelWireSelection()
        {
            pendingEndpoint = default;
            if (pendingWirePreview != null) Destroy(pendingWirePreview.gameObject);
            pendingWirePreview = null;
        }

        Pose HandPose => new Pose(handPoseSource.position, handPoseSource.rotation);

        void UpdateHeldPart()
        {
            if (heldPart == null || handPoseSource == null || workbench == null) return;
            Quaternion rotation = handPoseSource.rotation * handLocalRotation;
            Vector3 position = handPoseSource.position + handPoseSource.rotation * handLocalPosition;
            workbench.UpdateGrab(heldPart, new Pose(position, rotation));
        }

        bool TryGetEndpoint(RaycastHit hit, out WireEndpoint endpoint)
        {
            WorkbenchWireTerminal fixedEndpoint = hit.collider.GetComponentInParent<WorkbenchWireTerminal>();
            if (fixedEndpoint != null)
            {
                Transform anchor = fixedEndpoint.endpoint != null ? fixedEndpoint.endpoint : fixedEndpoint.transform;
                endpoint = new WireEndpoint { node = fixedEndpoint.nodeId, anchor = anchor };
                return true;
            }

            CircuitPart part = hit.collider.GetComponentInParent<CircuitPart>();
            if (part != null && part.terminalNodes != null && part.terminalNodes.Length >= 2)
            {
                Vector3 a = part.transform.TransformPoint(part.terminalOffsetA);
                Vector3 b = part.transform.TransformPoint(part.terminalOffsetB);
                int terminal = (hit.point - a).sqrMagnitude <= (hit.point - b).sqrMagnitude ? 0 : 1;
                endpoint = new WireEndpoint
                {
                    node = part.terminalNodes[terminal],
                    anchor = part.transform,
                    localPosition = terminal == 0 ? part.terminalOffsetA : part.terminalOffsetB
                };
                return true;
            }

            endpoint = default;
            return false;
        }

        bool TryFindFixedEndpoint(Transform origin, out WireEndpoint endpoint)
        {
            endpoint = default;
            if (fixedWireEndpoints == null) return false;
            float nearestDistance = 0.02f;
            float nearestDepth = rayDistance;
            bool found = false;
            Vector3 direction = origin.forward.normalized;
            foreach (FixedWireEndpoint candidate in fixedWireEndpoints)
            {
                if (candidate == null || candidate.endpoint == null || string.IsNullOrEmpty(candidate.nodeId)) continue;
                Vector3 offset = candidate.endpoint.position - origin.position;
                float depth = Vector3.Dot(offset, direction);
                if (depth < 0f || depth > rayDistance) continue;
                float distance = (offset - direction * depth).magnitude;
                if (distance >= nearestDistance || depth > nearestDepth) continue;
                nearestDistance = distance;
                nearestDepth = depth;
                endpoint = new WireEndpoint { node = candidate.nodeId, anchor = candidate.endpoint };
                found = true;
            }
            return found;
        }

        void DrawWire(WireEndpoint from, WireEndpoint to)
        {
            LineRenderer line = CreateWireLine("Workbench Wire");
            wireVisuals.Add(new WireSegment { from = from, to = to, line = line });
            SetWirePoints(line, from.Position, to.Position);
        }

        LineRenderer CreateWireLine(string objectName)
        {
            GameObject wire = new GameObject(objectName);
            LineRenderer line = wire.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.startWidth = wireWidth;
            line.endWidth = wireWidth;
            if (wireMaterial != null) line.sharedMaterial = wireMaterial;
            else line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = line.sharedMaterial.color;
            return line;
        }

        void UpdateWireVisuals()
        {
            for (int i = 0; i < wireVisuals.Count; i++)
            {
                WireSegment segment = wireVisuals[i];
                if (segment.line != null) SetWirePoints(segment.line, segment.from.Position, segment.to.Position);
            }
        }

        void UpdatePendingWirePreview()
        {
            if (pendingWirePreview == null || handPoseSource == null) return;
            Transform origin = rayOrigin != null ? rayOrigin : handPoseSource;
            Vector3 endpoint = origin.position + origin.forward * rayDistance;
            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, rayDistance, interactionLayers)) endpoint = hit.point;
            SetWirePoints(pendingWirePreview, pendingEndpoint.Position, endpoint);
        }

        void SetWirePoints(LineRenderer line, Vector3 from, Vector3 to)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, (from + to) * 0.5f + Vector3.up * wireArchHeight);
            line.SetPosition(2, to);
        }
    }
}
