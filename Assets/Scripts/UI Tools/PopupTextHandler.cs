using System.Collections;
using TMPro;
using UnityEngine;

namespace MyGame
{
    public class PopupTextHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        [Header("Motion")]
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseSpeed = 1.0f;

        [Header("Fade")]
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Billboard")]
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool keepUpright = true;

        [Tooltip("Enable if the text appears reversed (facing away).")]
        [SerializeField] private bool flipFacing = true;

        private Camera cam;
        private Color baseColor;
        private Coroutine anim;

        private void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            cam = Camera.main;

            if (text == null)
            {
                Debug.LogError("PopupTextHandler: No TMP_Text found on popup prefab (use TextMeshPro, not TextMeshProUGUI).", this);
                enabled = false;
                return;
            }
            // Force a predictable scale regardless of prefab authoring.
            text.margin = Vector4.zero;
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            if (text != null)
                baseColor = text.color;
        }

        public void Play(string message)
        {
            if (text == null) return;

            text.text = "+" + message;

            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Animate());
        }

        private void LateUpdate()
        {
            if (!faceCamera || cam == null) return;

            Vector3 dir = cam.transform.position - transform.position;

            if (keepUpright)
                dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                return;

            if (flipFacing)
                dir = -dir;

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private IEnumerator Animate()
        {
            float t = 0f;

            while (t < lifetime)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / lifetime);

                transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

                float a = alphaCurve.Evaluate(u);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}