using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BenchFlow
{
    /// <summary>
    /// VR Player Rig manager.
    /// Manages the Center Eye (Head Camera), Left Hand, and Right Hand tracking nodes.
    /// Automatically detects VR headsets and falls back smoothly to Desktop Preview if running without VR.
    /// </summary>
    public sealed class VRRig : MonoBehaviour
    {
        [Header("XR Nodes")]
        public Transform headTransform;
        public Camera centerEyeCamera;
        public VRHandInteractor leftHand;
        public VRHandInteractor rightHand;

        [Header("Desktop Fallback")]
        public DesktopPreviewInteractor desktopPreview;

        [Header("Comfort & Positioning")]
        public Vector3 standingBenchOffset = new Vector3(0f, 0f, -0.48f);
        public float headTargetHeight = 1.15f;

        InputDevice headDevice;
        bool isHeadsetPresent = false;

        void Awake()
        {
            if (centerEyeCamera == null) centerEyeCamera = GetComponentInChildren<Camera>();
            if (headTransform == null && centerEyeCamera != null) headTransform = centerEyeCamera.transform;

            if (leftHand == null) leftHand = GetComponentInChildren<VRHandInteractor>();
            if (desktopPreview == null) desktopPreview = FindAnyObjectByType<DesktopPreviewInteractor>();

            EnsureHandControllers();
            CheckHMDPresence();
        }

        void EnsureHandControllers()
        {
            if (leftHand == null)
            {
                GameObject lh = new GameObject("LeftHandController");
                lh.transform.SetParent(transform, false);
                lh.transform.localPosition = new Vector3(-0.15f, 0.05f, 0.25f);
                leftHand = lh.AddComponent<VRHandInteractor>();
                leftHand.handNode = XRNode.LeftHand;
            }

            if (rightHand == null)
            {
                GameObject rh = new GameObject("RightHandController");
                rh.transform.SetParent(transform, false);
                rh.transform.localPosition = new Vector3(0.15f, 0.05f, 0.25f);
                rightHand = rh.AddComponent<VRHandInteractor>();
                rightHand.handNode = XRNode.RightHand;
            }

            WorkbenchState wb = FindAnyObjectByType<WorkbenchState>();
            BreadboardGrid bg = FindAnyObjectByType<BreadboardGrid>();
            TutorialController tc = FindAnyObjectByType<TutorialController>();

            if (leftHand.workbench == null) leftHand.workbench = wb;
            if (leftHand.breadboard == null) leftHand.breadboard = bg;
            if (leftHand.tutorial == null) leftHand.tutorial = tc;

            if (rightHand.workbench == null) rightHand.workbench = wb;
            if (rightHand.breadboard == null) rightHand.breadboard = bg;
            if (rightHand.tutorial == null) rightHand.tutorial = tc;
        }

        void Start()
        {
            ApplyBenchPositioning();
        }

        void Update()
        {
            if (!headDevice.isValid)
            {
                headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            }

            if (headDevice.isValid)
            {
                isHeadsetPresent = true;
                if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                {
                    if (headTransform != null) headTransform.localPosition = pos;
                }
                if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                {
                    if (headTransform != null) headTransform.localRotation = rot;
                }
            }

            // Sync desktop preview state
            if (desktopPreview != null)
            {
                desktopPreview.enabled = !isHeadsetPresent || Application.isEditor;
            }
        }

        public void CheckHMDPresence()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            isHeadsetPresent = devices.Count > 0;
        }

        public void ApplyBenchPositioning()
        {
            transform.position = standingBenchOffset;
        }
    }
}
