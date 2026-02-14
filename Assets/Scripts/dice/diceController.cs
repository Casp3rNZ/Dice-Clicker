using UnityEngine;
using System.Collections;

namespace MyGame
{
    public class DiceController : MonoBehaviour
    {

        public event System.Action<int> OnDiceSettled;

        [Header("Pip Settings")]
        [SerializeField] private float pipSize = 0.08f;
        [SerializeField] private float spacing = 0.2f;

        [Header("Roll / Settle Detection")]
        [SerializeField] private float maxSettleWait = 8f;           // seconds (failsafe)

        [Header("Collision Audio")]
        [SerializeField] private float collisionCooldown = 0.1f;
        [SerializeField] private float minImpulse = 0.5f;
        private float _lastCollisionTime = -1f;

        private bool isRolling = false;
        public bool IsRolling => isRolling;
        private bool isInitialized = false;
        private Rigidbody rb;
        private Material diceMaterial;
        private Material pipMaterial;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (!isInitialized)
            {
                SetupDice();
                CreatePips();
                SetupPips();
                isInitialized = true;
            }
        }

        public void InitializeAppearance()
        {
            SetupDice();
            if (!isInitialized)
            {
                CreatePips();
                isInitialized = true;
            }
            SetupPips();
        }

        public void SetMaterial(Material material)
        {
            diceMaterial = material;
            SetupDice();
        }

        public void SetPipMaterial(Material material)
        {
            pipMaterial = material;
            SetupPips();
        }

        /// <summary>
        /// Sets up the dice renderer with the current material.
        /// </summary>
        void SetupDice()
        {
            Renderer diceRenderer = GetComponent<Renderer>();
            if (diceRenderer != null)
            {
                if (diceMaterial != null)
                    diceRenderer.sharedMaterial = diceMaterial;
            }
        }

        /// <summary>
        /// Applies the current pip material to all pip renderers.
        /// </summary>
        void SetupPips()
        {
            if (pipMaterial == null)
                return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                // Only apply to pip objects (avoid changing the dice body's renderer).
                if (renderer.gameObject != gameObject && renderer.gameObject.name == "Pip")
                    renderer.sharedMaterial = pipMaterial;
            }
        }

        /// <summary>
        /// Creates pip GameObjects as children of the dice, positioned according to standard dice layouts.
        /// </summary>
        void CreatePips()
        {
            // Standard dice layout
            int[] pipCounts = { 1, 6, 2, 5, 4, 3 };
            
            Vector3[] faceDirections = {
                Vector3.up, Vector3.down, Vector3.right,
                Vector3.left, Vector3.forward, Vector3.back
            };

            for (int i = 0; i < 6; i++)
            {
                CreatePipsForFace(faceDirections[i], pipCounts[i]);
            }
        }

        /// <summary>
        /// Creates pip GameObjects for a specific face of the dice based on the number of pips required, and positions them correctly on that face.
        /// </summary>
        /// <param name="faceDir">The direction the face is pointing.</param>
        /// <param name="pipCount">The number of pips to create on this face.</param>
        void CreatePipsForFace(Vector3 faceDir, int pipCount)
        {
            Vector3 faceUp = GetFaceUp(faceDir);
            Vector3 faceRight = GetFaceRight(faceDir);

            Vector2[] positions = CalculatePipPositions(pipCount, spacing);

            foreach (Vector2 pos in positions)
            {
                {
                    GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pip.name = "Pip";
                    pip.transform.SetParent(transform);
                    if (pipMaterial != null)
                        pip.GetComponent<Renderer>().sharedMaterial = pipMaterial;
                    PositionPip(pip.transform, faceDir, faceUp, faceRight, pos);
                    pip.transform.localScale = Vector3.one * pipSize;
                    Destroy(pip.GetComponent<Collider>());
                    pip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        /// <summary>
        /// Positions a single pip on the dice face based on the face direction, up/right vectors, and the local 2D position for that pip.
        /// </summary>
        /// <param name="pip">The transform of the pip to position.</param>
        /// <param name="faceDir">The forward direction the face is pointing.</param>
        /// <param name="faceUp">The up direction for the face.</param>
        /// <param name="faceRight">The right direction for the face.</param>
        /// <param name="localPos">The local 2D position for the pip on the face.</param>
        void PositionPip(Transform pip, Vector3 faceDir, Vector3 faceUp, Vector3 faceRight, Vector2 localPos)
        {
            Vector3 position = faceDir * 0.51f; // Slightly above surface
            position += faceRight * localPos.x;
            position += faceUp * localPos.y;
            pip.localPosition = position;
        }

        /// <summary>
        /// Calculates the local 2D positions for pips on a dice face based on the number of pips and the desired spacing. 
        /// Returns an array of Vector2 positions that can be used to position pip GameObjects on the face.
        /// </summary>
        /// <param name="count">The number of pips on the face.</param>
        /// <param name="spacing">The spacing between pips.</param>
        /// <returns>An array of Vector2 positions for the pips.</returns>
        Vector2[] CalculatePipPositions(int count, float spacing)
        {
            switch (count)
            {
                case 1: return new Vector2[] { Vector2.zero };
                case 2: return new Vector2[] { 
                    new Vector2(-spacing, spacing), 
                    new Vector2(spacing, -spacing) 
                };
                case 3: return new Vector2[] { 
                    new Vector2(-spacing, spacing), 
                    Vector2.zero, 
                    new Vector2(spacing, -spacing) 
                };
                case 4: return new Vector2[] { 
                    new Vector2(-spacing, spacing), 
                    new Vector2(spacing, spacing), 
                    new Vector2(-spacing, -spacing), 
                    new Vector2(spacing, -spacing) 
                };
                case 5: return new Vector2[] { 
                    new Vector2(-spacing, spacing), 
                    new Vector2(spacing, spacing), 
                    Vector2.zero, 
                    new Vector2(-spacing, -spacing), 
                    new Vector2(spacing, -spacing) 
                };
                case 6: return new Vector2[] { 
                    new Vector2(-spacing, spacing), 
                    new Vector2(spacing, spacing), 
                    new Vector2(-spacing, 0), 
                    new Vector2(spacing, 0), 
                    new Vector2(-spacing, -spacing), 
                    new Vector2(spacing, -spacing) 
                };
                default: return new Vector2[0];
            }
        }

        Vector3 GetFaceUp(Vector3 faceDir)
        {
            if (faceDir == Vector3.up || faceDir == Vector3.down)
                return Vector3.forward;
            return Vector3.up;
        }

        Vector3 GetFaceRight(Vector3 faceDir)
        {
            return Vector3.Cross(faceDir, GetFaceUp(faceDir));
        }


        /// <summary>
        /// Rolls the dice by applying a random force and torque.
        /// </summary>
        public void Roll()
        {
            if (isRolling)
            {
                return;
            }
            if (rb == null)
            {
                UnityEngine.Debug.LogWarning("DiceController: No Rigidbody found.");
                return;
            }

            AudioManager.Instance.PlaySFX_DiceRoll();
            isRolling = true;
            Vector3 randomForce = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(15f, 50f),
                Random.Range(-5f, 5f));
            Vector3 randomTorque = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f));

            rb.AddForce(randomForce, ForceMode.Impulse);
            rb.AddTorque(randomTorque, ForceMode.Impulse);
            StopAllCoroutines();
            StartCoroutine(WaitForSettleThenReport());
        }

        /// <summary>
        /// Waits until the dice has settled, then determines the top face value and invokes the OnDiceSettled event with that value.
        /// </summary>
        private IEnumerator WaitForSettleThenReport()
        {
            float elapsed = 0f;

            while (elapsed < maxSettleWait)
            {
                elapsed += Time.deltaTime;

                if (rb.IsSleeping()) break;

                yield return null;
            }

            int result = GetTopFaceValue();
            isRolling = false;
            OnDiceSettled?.Invoke(result);
        }

        private int GetTopFaceValue()
        {
            int[] faceValues = { 1, 6, 2, 5, 4, 3 };
            Vector3[] localFaceDirs =
            {
                Vector3.up, Vector3.down,
                Vector3.right, Vector3.left,
                Vector3.forward, Vector3.back
            };

            float bestDot = float.NegativeInfinity;
            int bestValue = -1;

            for (int i = 0; i < localFaceDirs.Length; i++)
            {
                // Convert the local face normal to world space
                Vector3 worldDir = transform.TransformDirection(localFaceDirs[i]).normalized;
                float dot = Vector3.Dot(worldDir, Vector3.up);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestValue = faceValues[i];
                }
            }

            return bestValue;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (AudioManager.Instance == null) return;
            if (!collision.gameObject.CompareTag("ground") && !collision.gameObject.CompareTag("dice"))
                return;

            // Skip weak impacts and enforce cooldown to avoid SFX spam
            if (collision.relativeVelocity.sqrMagnitude < minImpulse * minImpulse) return;
            if (Time.time - _lastCollisionTime < collisionCooldown) return;
            _lastCollisionTime = Time.time;

            // Scale volume by impact strength (clamped 0..1)
            float impactSpeed = collision.relativeVelocity.magnitude;
            float volume = Mathf.Clamp01(impactSpeed / 8f);
            float pitch = Mathf.Clamp(impactSpeed / 10f, 0.8f, 1.2f);

            if (!collision.gameObject.CompareTag("ground"))
            {
                AudioManager.Instance.PlaySFX_DiceToSurfaceCollision(volume, 0.2f, pitch);
            } else {
                AudioManager.Instance.PlaySFX_DiceToDiceCollission(volume, 0.2f, pitch);
            }
        }
    }
}