using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Input boundary for Meta Interaction SDK, desktop preview, or future MR adapters.
    /// Adapters call these methods; circuit and tutorial state do not depend on a headset SDK.
    /// </summary>
    public interface IWorkbenchInteractionPort
    {
        void BeginGrab(CircuitPart part, Pose handPose);
        void UpdateGrab(CircuitPart part, Pose handPose);
        void EndGrab(CircuitPart part, Vector3 releasePosition);
        void ConnectWire(string terminalA, string terminalB);
    }

}
