using UnityEngine;

namespace BenchFlow
{
    /// <summary>A fixed electrical endpoint that can be selected while wiring in VR.</summary>
    public sealed class WorkbenchWireTerminal : MonoBehaviour
    {
        public string nodeId;
        public Transform endpoint;

        public Vector3 Position => (endpoint != null ? endpoint : transform).position;
    }
}
