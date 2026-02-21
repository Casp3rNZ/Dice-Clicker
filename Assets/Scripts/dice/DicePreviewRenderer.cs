using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    /// <summary>
    /// Renders 3D dice previews into RenderTextures for use in UI (RawImage).
    /// Place this on a GameObject in the scene. 
    /// It creates a hidden camera and a staging area far from the playfield to render each dice type once.
    /// Dice slowly rotate in the thumbnails via round-robin re-rendering (1 per frame).
    ///
    /// Usage:
    ///   Texture tex = DicePreviewRenderer.Instance.GetPreview(shopItem);
    ///   rawImage.texture = tex;
    /// </summary>
    public class DicePreviewRenderer : MonoBehaviour
    {
        public static DicePreviewRenderer Instance { get; private set; }

        [Header("Preview Settings")]
        [Tooltip("The dice prefab to instantiate for previews.")]
        [SerializeField] private GameObject dicePrefab;

        [Tooltip("Resolution of each preview thumbnail (square).")]
        [SerializeField] private int resolution = 128;

        [Tooltip("Background color behind the dice (use alpha 0 for transparent).")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);

        [Header("Stage Positioning")]
        [Tooltip("World-space origin for the hidden preview stage. Place far from gameplay area.")]
        [SerializeField] private Vector3 stageOrigin = new Vector3(0f, -500f, 0f);

        [Tooltip("Distance between the camera and the dice.")]
        [SerializeField] private float cameraDistance = 2.5f;

        [Tooltip("Spacing between dice on the hidden stage (to keep them apart).")]
        [SerializeField] private float stageSpacing = 5f;

        [Header("Animation")]
        [Tooltip("Rotation speed in degrees/sec per axis.")]
        [SerializeField] private Vector3 rotationSpeed = new Vector3(8f, 20f, 5f);

        [Tooltip("Seconds between preview re-renders (0.05 = 20 FPS).")]
        [SerializeField] private float renderInterval = 0.05f;

        [Header("Lighting")]
        [Tooltip("Optional: override light intensity for the preview. 0 = use scene lighting.")]
        [SerializeField] private float lightIntensity = 1.2f;

        // Layer used exclusively for preview rendering (set to an unused layer index).
        private const int PreviewLayer = 31;

        private Camera _previewCamera;
        private Light _previewLight;

        // Persistent dice instances for animated previews
        private readonly List<PreviewEntry> _entries = new List<PreviewEntry>();
        private float _renderTimer = 0f;

        private class PreviewEntry
        {
            public int itemId;
            public GameObject diceGO;
            public RenderTexture rt;
            public Vector3 stagePos;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreatePreviewCamera();
            CreatePreviewLight();
        }

        private void OnDestroy()
        {
            foreach (var entry in _entries)
            {
                if (entry.diceGO != null) Destroy(entry.diceGO);
                if (entry.rt != null) entry.rt.Release();
            }
            _entries.Clear();

            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            if (_entries.Count == 0) return;

            float dt = Time.deltaTime;

            // Rotate ALL dice every frame (cheap transform ops)
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.diceGO != null)
                    e.diceGO.transform.Rotate(rotationSpeed * dt, Space.World);
            }

            // Throttle rendering — batch all previews at ~20 FPS
            _renderTimer += dt;
            if (_renderTimer < renderInterval) return;
            _renderTimer = 0f;

            for (int j = 0; j < _entries.Count; j++)
            {
                var e = _entries[j];
                if (e.diceGO != null && e.rt != null)
                {
                    _previewCamera.targetTexture = e.rt;
                    _previewCamera.transform.position = e.stagePos + Vector3.back * cameraDistance + Vector3.up * 0.5f;
                    _previewCamera.transform.LookAt(e.stagePos);
                    _previewCamera.Render();
                }
            }
            _previewCamera.targetTexture = null;
        }

        /// <summary>
        /// Returns a RenderTexture containing a live, slowly-rotating preview of the dice of <c>item</c>.
        /// The first call creates the persistent dice instance; subsequent calls return the same texture.
        /// </summary>
        /// <param name="item">The shop item representing the dice.</param>
        /// <returns>RenderTexture containing the live preview of the dice.</returns>
        public Texture GetPreview(ShopItem item)
        {
            if (item == null) return null;

            // Check for existing entry
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].itemId == item.Id)
                    return _entries[i].rt;
            }

            // Create new persistent preview
            PreviewEntry entry = CreatePreviewEntry(item);
            _entries.Add(entry);
            return entry.rt;
        }

        /// <summary>
        /// Instantiates a dice GameObject for the given item, and creates a RenderTexture preview.
        /// </summary>
        /// <param name="item">The shop item representing the dice.</param>
        /// <returns>A PreviewEntry containing the dice GameObject and its RenderTexture.</returns>
        private PreviewEntry CreatePreviewEntry(ShopItem item)
        {
            // Position each die at a unique offset on the stage so they don't overlap
            Vector3 pos = stageOrigin + Vector3.right * (_entries.Count * stageSpacing);

            GameObject diceGO = Instantiate(dicePrefab, pos, Quaternion.Euler(25f, -35f, 15f));

            // Remove physics entirely — preview dice don't need it
            if (diceGO.TryGetComponent<Rigidbody>(out var rb))
                Destroy(rb);

            diceGO.tag = "Untagged";

            // Remove all colliders (including pip colliders) — no physics interaction needed
            foreach (var col in diceGO.GetComponentsInChildren<Collider>())
                Destroy(col);

            // Apply materials and generate pips
            if (diceGO.TryGetComponent<DiceController>(out var dc))
            {
                dc.SetMaterial(item.diceMaterial);
                dc.SetPipMaterial(item.pipMaterial);
                dc.InitializeAppearance();
                dc.enabled = false;
            }

            // Set layer after pips are created
            SetLayerRecursive(diceGO, PreviewLayer);

            // Create RenderTexture — small, no MSAA for mobile perf
            RenderTexture rt = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1;
            rt.Create();

            // Initial render
            _previewCamera.targetTexture = rt;
            _previewCamera.transform.position = pos + Vector3.back * cameraDistance + Vector3.up * 0.5f;
            _previewCamera.transform.LookAt(pos);
            _previewCamera.Render();
            _previewCamera.targetTexture = null;

            return new PreviewEntry
            {
                itemId = item.Id,
                diceGO = diceGO,
                rt = rt,
                stagePos = pos
            };
        }

        /// <summary>
        /// Creates hidden camera used for rendering dice previews.
        /// </summary>
        private void CreatePreviewCamera()
        {
            GameObject camGO = new GameObject("DicePreviewCamera");
            camGO.transform.SetParent(transform);
            camGO.transform.position = stageOrigin + Vector3.back * cameraDistance + Vector3.up * 0.5f;

            _previewCamera = camGO.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = backgroundColor;
            _previewCamera.cullingMask = 1 << PreviewLayer;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 50f;
            _previewCamera.fieldOfView = 30f;
            _previewCamera.enabled = false; // Only renders on demand via .Render()
            _previewCamera.transform.LookAt(stageOrigin);
        }

        /// <summary>
        /// Creates a directional light for the dice preview scene. 
        /// </summary>
        private void CreatePreviewLight()
        {
            if (lightIntensity <= 0f) return;

            GameObject lightGO = new GameObject("DicePreviewLight");
            lightGO.transform.SetParent(transform);
            lightGO.transform.position = stageOrigin + new Vector3(-2f, 3f, -2f);
            lightGO.transform.LookAt(stageOrigin);
            lightGO.layer = PreviewLayer;

            _previewLight = lightGO.AddComponent<Light>();
            _previewLight.type = LightType.Directional;
            _previewLight.intensity = lightIntensity;
            _previewLight.cullingMask = 1 << PreviewLayer;
            _previewLight.shadows = LightShadows.None;
        }

        /// <summary>
        /// Recursively sets the layer of a GameObject and all its children.
        /// This ensures the entire dice and all its pips are rendered by the preview camera and not culled by other cameras.
        /// </summary>
        /// <param name="go">The GameObject whose layer is to be set.</param>
        /// <param name="layer">The layer to set.</param>
        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
