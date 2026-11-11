using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BenchFlow.XR
{
    /// <summary>Minimal OpenXR camera rig. It leaves the desktop preview camera in place when no XR display is active.</summary>
    public sealed class QuestXRPlayerRig : MonoBehaviour
    {
        const string RigResourcePath = "BenchFlow/QuestXRPlayerRig";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureRigExists()
        {
            if (FindFirstObjectByType<QuestXRPlayerRig>() != null) return;

            GameObject prefab = Resources.Load<GameObject>(RigResourcePath);
            if (prefab != null)
            {
                Instantiate(prefab);
                return;
            }

            new GameObject("Quest XR Player Rig").AddComponent<QuestXRPlayerRig>();
        }
        Camera xrCamera;
        InputDevice headDevice;
        readonly List<Camera> disabledCameras = new List<Camera>();
        bool wasActive;

        void Awake()
        {
            GameObject cameraObject = new GameObject("XR Head Camera");
            cameraObject.transform.SetParent(transform, false);
            xrCamera = cameraObject.AddComponent<Camera>();
            xrCamera.tag = "MainCamera";
            xrCamera.nearClipPlane = 0.01f;
            xrCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            xrCamera.enabled = false;
        }

        void Update()
        {
            bool xrActive = XRSettings.isDeviceActive;
            if (xrActive != wasActive)
            {
                if (xrActive) DisableOtherMainCameras();
                else RestoreOtherMainCameras();
                wasActive = xrActive;
            }
            xrCamera.enabled = xrActive;

            if (!xrActive) return;
            if (!headDevice.isValid) headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!headDevice.isValid) return;

            if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                xrCamera.transform.localPosition = position;
            if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                xrCamera.transform.localRotation = rotation;
        }

        void OnDestroy() => RestoreOtherMainCameras();

        void DisableOtherMainCameras()
        {
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera == xrCamera || !camera.enabled || camera.cameraType == CameraType.Preview) continue;
                disabledCameras.Add(camera);
                camera.enabled = false;
            }
        }

        void RestoreOtherMainCameras()
        {
            foreach (Camera camera in disabledCameras)
                if (camera != null) camera.enabled = true;
            disabledCameras.Clear();
        }
    }
}
