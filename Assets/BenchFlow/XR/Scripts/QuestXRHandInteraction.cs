using System.Collections.Generic;
using BenchFlow;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace BenchFlow.XR
{
    /// <summary>Reads XR Hands joints and forwards pinch-grab gestures through IWorkbenchInteractionPort.</summary>
    public sealed class QuestXRHandInteraction : MonoBehaviour
    {
        [Tooltip("Optional explicit IWorkbenchInteractionPort implementation. When empty, one is found in the loaded scene.")]
        public MonoBehaviour interactionPortComponent;
        [Min(0.01f)] public float pinchThreshold = 0.025f;
        [Min(0.01f)] public float partReach = 0.09f;

        readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
        readonly HandState left = new HandState();
        readonly HandState right = new HandState();
        IWorkbenchInteractionPort port;

        void Update()
        {
            ResolvePort();
            UpdateHands();
        }

        void OnDisable()
        {
            EndGrab(left);
            EndGrab(right);
        }

        void ResolvePort()
        {
            if (interactionPortComponent != null)
            {
                port = interactionPortComponent as IWorkbenchInteractionPort;
                return;
            }

            if (port != null) return;
            foreach (MonoBehaviour candidate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is IWorkbenchInteractionPort found)
                {
                    interactionPortComponent = candidate;
                    port = found;
                    return;
                }
            }
        }

        void UpdateHands()
        {
            handSubsystems.Clear();
            SubsystemManager.GetSubsystems(handSubsystems);
            XRHandSubsystem handSubsystem = handSubsystems.Find(subsystem => subsystem.running);
            if (handSubsystem == null)
            {
                left.pinched = right.pinched = false;
                EndGrab(left);
                EndGrab(right);
                return;
            }

            UpdateHand(handSubsystem.leftHand, left);
            UpdateHand(handSubsystem.rightHand, right);
        }

        void UpdateHand(XRHand hand, HandState state)
        {
            XRHandJoint thumb = hand.GetJoint(XRHandJointID.ThumbTip);
            XRHandJoint index = hand.GetJoint(XRHandJointID.IndexTip);
            bool hasThumb = thumb.TryGetPose(out Pose thumbPose);
            bool hasIndex = index.TryGetPose(out Pose indexPose);
            bool tracked = hand.isTracked && hasThumb && hasIndex;
            bool pinched = tracked && Vector3.Distance(thumbPose.position, indexPose.position) <= pinchThreshold;
            Pose handPose = tracked
                ? new Pose((thumbPose.position + indexPose.position) * 0.5f, indexPose.rotation)
                : default;

            if (!pinched && state.pinched) EndGrab(state);
            if (pinched && !state.pinched && port != null) BeginGrab(state, handPose);
            if (pinched && state.part != null)
            {
                Pose heldPose = new Pose(
                    handPose.position + handPose.rotation * state.grabLocalPosition,
                    handPose.rotation * state.grabLocalRotation);
                port?.UpdateGrab(state.part, heldPose);
            }
            state.pinched = pinched;
        }

        void BeginGrab(HandState state, Pose handPose)
        {
            CircuitPart closest = null;
            float bestDistance = partReach;
            foreach (CircuitPart part in FindObjectsByType<CircuitPart>(FindObjectsSortMode.None))
            {
                if (part == null) continue;
                Collider[] colliders = part.GetComponentsInChildren<Collider>();
                float distance = float.MaxValue;
                foreach (Collider collider in colliders)
                    if (collider != null) distance = Mathf.Min(distance, Vector3.Distance(collider.ClosestPoint(handPose.position), handPose.position));
                if (distance < bestDistance)
                {
                    closest = part;
                    bestDistance = distance;
                }
            }

            if (closest == null) return;
            state.part = closest;
            Quaternion inverseHandRotation = Quaternion.Inverse(handPose.rotation);
            state.grabLocalPosition = inverseHandRotation * (closest.transform.position - handPose.position);
            state.grabLocalRotation = inverseHandRotation * closest.transform.rotation;
            port.BeginGrab(state.part, handPose);
        }

        void EndGrab(HandState state)
        {
            if (state.part == null) return;
            port?.EndGrab(state.part, state.part.transform.position);
            state.part = null;
        }

        sealed class HandState
        {
            public bool pinched;
            public CircuitPart part;
            public Vector3 grabLocalPosition;
            public Quaternion grabLocalRotation;
        }
    }
}
