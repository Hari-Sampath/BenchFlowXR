using UnityEngine;

namespace BenchFlow
{
    /// <summary>Identifies a physical terminal target used to start or end a jumper wire.</summary>
    public sealed class WireEndpointAnchor : MonoBehaviour
    {
        public string nodeId;

        public Vector3 WorldPosition => transform.position;
    }
}
