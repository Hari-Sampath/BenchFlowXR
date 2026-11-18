using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Displays a semi-transparent holographic ghost silhouette of components
    /// seated at the target breadboard holes while the user is holding the part.
    /// Provides magnetic alignment matching the reference video.
    /// </summary>
    public sealed class GhostPreview : MonoBehaviour
    {
        public static GhostPreview Instance { get; private set; }

        public Material ghostMaterial;
        public Color ghostColor = new Color(0.04f, 0.22f, 0.85f, 0.35f);

        GameObject ghostLed;
        GameObject ghostResistor;

        void Awake()
        {
            Instance = this;
            CreateGhostMaterial();
        }

        void CreateGhostMaterial()
        {
            if (ghostMaterial != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            ghostMaterial = new Material(shader) { color = ghostColor };
            ghostMaterial.EnableKeyword("_EMISSION");
            ghostMaterial.SetColor("_EmissionColor", ghostColor * 1.5f);
        }

        public void SetupGhosts(BreadboardGrid board, float scale)
        {
            if (board == null) return;
            CreateGhostMaterial();

            // 1. Ghost LED at 14c and 16c
            if (ghostLed == null)
            {
                Vector3 p14c = board.HolePosition(board.GetHole("14c"));
                Vector3 p16c = board.HolePosition(board.GetHole("16c"));
                Vector3 mid = (p14c + p16c) * 0.5f;

                ghostLed = new GameObject("Ghost_LED");
                ghostLed.transform.position = mid;

                GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.transform.SetParent(ghostLed.transform, false);
                dome.transform.localScale = new Vector3(0.0055f * scale, 0.008f * scale, 0.0055f * scale);
                dome.transform.localPosition = new Vector3(0, 0.008f * scale, 0);
                RemoveCollider(dome);
                dome.GetComponent<Renderer>().sharedMaterial = ghostMaterial;

                ghostLed.SetActive(false);
            }

            // 2. Ghost Resistor at 8d and 14d
            if (ghostResistor == null)
            {
                Vector3 p8d = board.HolePosition(board.GetHole("8d"));
                Vector3 p14d = board.HolePosition(board.GetHole("14d"));
                Vector3 mid = (p8d + p14d) * 0.5f;

                ghostResistor = new GameObject("Ghost_Resistor");
                ghostResistor.transform.position = mid;

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.transform.SetParent(ghostResistor.transform, false);
                body.transform.localScale = new Vector3(0.0035f * scale, 0.006f * scale, 0.0035f * scale);
                body.transform.localRotation = Quaternion.Euler(0, 0, 90);
                body.transform.localPosition = new Vector3(0, 0.004f * scale, 0);
                RemoveCollider(body);
                body.GetComponent<Renderer>().sharedMaterial = ghostMaterial;

                ghostResistor.SetActive(false);
            }
        }

        static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        public void ShowGhost(string componentId, bool show)
        {
            if (componentId == "LED" && ghostLed != null)
            {
                ghostLed.SetActive(show);
            }
            else if (componentId == "RESISTOR" && ghostResistor != null)
            {
                ghostResistor.SetActive(show);
            }
        }

        public void HideAll()
        {
            if (ghostLed != null) ghostLed.SetActive(false);
            if (ghostResistor != null) ghostResistor.SetActive(false);
        }
    }
}
