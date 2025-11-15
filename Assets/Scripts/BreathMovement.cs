using UnityEngine;

public class BreathMovement : MonoBehaviour
{
    [Header("Flower Setup")]
    [Tooltip("Root object that has your 8 circles as children (or is itself the flower).")]
    public Transform flowerRoot;

    [Tooltip("If true, this script will use its own transform as the flower root.")]
    public bool useSelfAsRoot = true;

    [Header("Layout")]
    [Tooltip("Distance from center for each circle to create the flower shape at full inhale.")]
    public float petalRadius = 0.5f;

    private Transform[] _circles;
    private Vector3 _baseRootScale;
    private Vector3[] _circleBaseScales;
    private Vector3[] _circleTargetPositions; // positions when fully expanded

    [Header("Breathing Shape")]
    [Tooltip("Smallest flower scale (exhale).")]
    public float minBreathScale = 0.7f;

    [Tooltip("Largest flower scale (inhale).")]
    public float maxBreathScale = 1.3f;

    [Tooltip("Extra per-circle scale variation (for layered flower feel).")]
    public float circlePulseStrength = 0.06f;

    [Header("Base Timing")]
    [Tooltip("Breath period (seconds) when movementSpeed = 0 (very calm).")]
    public float calmBreathPeriod = 8f;

    [Tooltip("Breath period (seconds) when movementSpeed = maxMovementSpeed (fastest).")]
    public float fastBreathPeriod = 3f;

    [Header("Movement → Speed Mapping")]
    [Tooltip("Movement speed value at which we’re considered 'still'.")]
    public float minMovementSpeed = 0f;

    [Tooltip("Movement speed value at which we’re at max breathing speed.")]
    public float maxMovementSpeed = 2f;

    [Tooltip("Current movement speed (you update this from another script).")]
    public float movementSpeed = 0f;

    [Header("Easing & Extras")]
    [Tooltip("Easing for the breathing in/out (0–1).")]
    public AnimationCurve breathCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Slow rotation of the flower (deg/sec). Set 0 to disable.")]
    public float rotationSpeed = 5f;

    private float _t;

    private void Awake()
    {
        // Decide flower root
        if (useSelfAsRoot || flowerRoot == null)
            flowerRoot = transform;

        _baseRootScale = flowerRoot.localScale;

        int childCount = flowerRoot.childCount;
        _circles = new Transform[childCount];
        _circleBaseScales = new Vector3[childCount];
        _circleTargetPositions = new Vector3[childCount];

        // Compute target positions (expanded flower shape) but start collapsed at center
        for (int i = 0; i < childCount; i++)
        {
            Transform circle = flowerRoot.GetChild(i);
            _circles[i] = circle;

            // Cache original scale (so we can preserve Z scale later)
            _circleBaseScales[i] = circle.localScale;

            float angleDeg = (360f / childCount) * i;

            // Start along Y axis, then rotate around Z to form the ring
            Vector3 baseOffset = Vector3.up * petalRadius;
            Vector3 rotatedOffset = Quaternion.Euler(0f, 0f, angleDeg) * baseOffset;

            _circleTargetPositions[i] = rotatedOffset; // position at full inhale

            // Start collapsed – all circles stacked at center
            circle.localPosition = Vector3.zero;
            circle.localRotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        if (flowerRoot == null || _circles == null || _circles.Length == 0)
            return;

        // 1) Map movementSpeed → current breath period
        float normalizedSpeed = Mathf.InverseLerp(minMovementSpeed, maxMovementSpeed, movementSpeed);
        float breathPeriod = Mathf.Lerp(calmBreathPeriod, fastBreathPeriod, normalizedSpeed);
        breathPeriod = Mathf.Max(breathPeriod, 0.001f);

        // 2) Advance internal time
        _t += Time.deltaTime;
        float phase = (_t % breathPeriod) / breathPeriod;  // 0..1

        // 3) Smooth sinusoidal breathing 0..1, then easing
        float sinPhase = (Mathf.Sin(phase * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) * 0.5f;
        float eased = breathCurve.Evaluate(sinPhase);

        // eased ~ 0 → exhale (collapsed)
        // eased ~ 1 → inhale (expanded)

        // 4) Scale whole flower (breathing) – ONLY X & Y, keep Z
        float flowerScale = Mathf.Lerp(minBreathScale, maxBreathScale, eased);
        flowerRoot.localScale = new Vector3(
            _baseRootScale.x * flowerScale,
            _baseRootScale.y * flowerScale,
            _baseRootScale.z               // Z unchanged
        );

        // 5) Optional slow rotation of whole flower
        if (Mathf.Abs(rotationSpeed) > 0.01f)
        {
            flowerRoot.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        // 6) Animate circle positions: center (exhale) → ring (inhale)
        for (int i = 0; i < _circles.Length; i++)
        {
            Transform circle = _circles[i];
            if (circle == null) continue;

            // Position: from center (Vector3.zero) to precomputed ring position
            Vector3 targetPos = _circleTargetPositions[i];
            Vector3 pos = Vector3.Lerp(Vector3.zero, targetPos, eased);
            circle.localPosition = new Vector3(pos.x, pos.y, circle.localPosition.z); // keep Z position
        }

        // 7) Per-circle subtle pulse – ONLY X & Y, keep original Z scale
        for (int i = 0; i < _circles.Length; i++)
        {
            Transform circle = _circles[i];
            if (circle == null) continue;

            float offset = (float)i / _circles.Length;  // 0..1 per circle
            float circlePhase = (phase + offset * 0.25f) % 1f;
            float circleSin = (Mathf.Sin(circlePhase * Mathf.PI * 2f) + 1f) * 0.5f;

            float extraScale = 1f + (circleSin - 0.5f) * 2f * circlePulseStrength;

            Vector3 baseScale = _circleBaseScales[i];
            circle.localScale = new Vector3(
                baseScale.x * extraScale,
                baseScale.y * extraScale,
                baseScale.z                // Z unchanged
            );
        }
    }

    /// <summary>
    /// Call this from your movement/experiment code to update the current speed.
    /// </summary>
    public void SetMovementSpeed(float newSpeed)
    {
        movementSpeed = Mathf.Max(newSpeed, 0f);
    }


    /// <summary>
    /// Call this from your movement/experiment code to enable or disable the asset.
    /// </summary>
    public void ToggleFlowerState(bool showOrHideFlower)
    {
        gameObject.SetActive(showOrHideFlower);
    }
}