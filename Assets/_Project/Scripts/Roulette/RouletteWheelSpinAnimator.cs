using System;
using System.Collections;
using UnityEngine;

public class RouletteWheelSpinAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform wheelSpace;
    [SerializeField] private Transform ball;

    [Header("Wheel Type Visibility")]
    [SerializeField] private GameObject euWheelRoot;
    [SerializeField] private GameObject usWheelRoot;

    [Header("Pocket References")]
    [SerializeField] private Transform euPocketParent;
    [SerializeField] private Transform usPocketParent;

    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve wheelSpinCurve = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),
        new Keyframe(0.70f, 0.85f),
        new Keyframe(1.00f, 1.00f)
    );
    [SerializeField] private AnimationCurve ballOrbitCurve = new AnimationCurve(
        new Keyframe(0.00f, 1.00f),
        new Keyframe(0.30f, 0.95f),
        new Keyframe(0.70f, 0.60f),
        new Keyframe(1.00f, 0.00f)
    );
    [SerializeField] private AnimationCurve ballDropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float wheelWarmupDuration = 0.5f;
    [SerializeField] private float mainSpinDuration = 2.5f;
    [SerializeField] private float coastDuration = 1.5f;
    [SerializeField] private float dropDuration = 0.8f;
    [SerializeField] private float settleDuration = 0.4f;
    [SerializeField] private float ballPositionDuration = 0.3f;

    [Header("Spin Speeds")]
    [SerializeField] private float wheelSpinCount = 4f;
    [SerializeField] private float ballOrbitCount = 6f;
    [SerializeField] private float wheelBackspinAngle = -15f;
    [SerializeField] private float wheelWarmupForwardAngle = 30f;

    [Header("Ball Settings")]
    [SerializeField] private float spinHeight = 2.2f;
    [SerializeField] private float landingYOffset = 0.15f;
    [SerializeField] private float orbitWobbleAmplitude = 0.03f;
    [SerializeField] private float orbitWobbleFrequency = 4f;
    [SerializeField] private float defaultOrbitRadius = 1.5f;

    [Header("Random Variation")]
    [SerializeField] private float spinCountVariation = 1f;
    [SerializeField] private float orbitCountVariation = 1.5f;
    [SerializeField] private float durationVariation = 0.5f;

    public event Action OnBallDropStarted;
    public bool IsSpinAnimationPlaying => spinCoroutine != null;
    public float ExpectedSpinDurationSeconds => CalculateExpectedSpinDuration(GetPositiveDuration(mainSpinDuration + Mathf.Max(0f, durationVariation)));

    private Transform[] euPocketTransforms;
    private Transform[] usPocketTransforms;
    private Transform[] currentPocketTransforms;
    private Quaternion initialWheelRotation;
    private Coroutine spinCoroutine;

    private void Awake()
    {
        if (wheelSpace != null)
            initialWheelRotation = wheelSpace.localRotation;

        CachePocketTransforms();
        SetWheelType(RouletteWheelType.European);
    }

    private void CachePocketTransforms()
    {
        if (euPocketParent != null)
        {
            euPocketTransforms = new Transform[euPocketParent.childCount];
            for (int i = 0; i < euPocketParent.childCount; i++)
                euPocketTransforms[i] = euPocketParent.GetChild(i);
        }
        else
        {
            euPocketTransforms = Array.Empty<Transform>();
        }

        if (usPocketParent != null)
        {
            usPocketTransforms = new Transform[usPocketParent.childCount];
            for (int i = 0; i < usPocketParent.childCount; i++)
                usPocketTransforms[i] = usPocketParent.GetChild(i);
        }
        else
        {
            usPocketTransforms = Array.Empty<Transform>();
        }
    }

    public void SetWheelType(RouletteWheelType wheelType)
    {
        bool isAmerican = wheelType == RouletteWheelType.American;

        currentPocketTransforms = isAmerican ? usPocketTransforms : euPocketTransforms;

        if (euWheelRoot != null)
            euWheelRoot.SetActive(!isAmerican);

        if (usWheelRoot != null)
            usWheelRoot.SetActive(isAmerican);
    }

    public float PlaySpin(RouletteSlot winningSlot)
    {
        float effectiveWheelSpinCount = wheelSpinCount + UnityEngine.Random.Range(-spinCountVariation, spinCountVariation);
        float effectiveBallOrbitCount = ballOrbitCount + UnityEngine.Random.Range(-orbitCountVariation, orbitCountVariation);
        float effectiveMainSpinDuration = GetPositiveDuration(mainSpinDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));
        float expectedDuration = CalculateExpectedSpinDuration(effectiveMainSpinDuration);

        StopSpinAnimation();

        if (!CanPlaySpinVisual(winningSlot))
            return 0f;

        try
        {
            spinCoroutine = StartCoroutine(SpinAnimationRoutine(winningSlot, effectiveWheelSpinCount, effectiveBallOrbitCount, effectiveMainSpinDuration));
        }
        catch (Exception exception)
        {
            Debug.LogError($"RouletteWheelSpinAnimator: Spin visual could not start: {exception.Message}");
            spinCoroutine = null;
            return 0f;
        }

        return expectedDuration;
    }

    public void StopSpinAnimation()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }
    }

    private IEnumerator SpinAnimationRoutine(RouletteSlot winningSlot, float effectiveWheelSpinCount, float effectiveBallOrbitCount, float effectiveMainSpinDuration)
    {
        IEnumerator routine = AnimateSpin(winningSlot, effectiveWheelSpinCount, effectiveBallOrbitCount, effectiveMainSpinDuration);

        while (true)
        {
            bool hasNext = false;
            object current = null;

            try
            {
                hasNext = routine.MoveNext();
                if (hasNext)
                    current = routine.Current;
            }
            catch (Exception exception)
            {
                Debug.LogError($"RouletteWheelSpinAnimator: Spin visual failed with exception: {exception.Message}");
                break;
            }

            if (!hasNext)
                break;

            yield return current;
        }

        spinCoroutine = null;
    }

    private IEnumerator AnimateSpin(RouletteSlot winningSlot, float effectiveWheelSpinCount, float effectiveBallOrbitCount, float effectiveMainSpinDuration)
    {
        IEnumerator phase = SpinRoutine();                                             // Phase 0: wheel warmup
        while (phase.MoveNext())
            yield return phase.Current;

        phase = BallReleaseRoutine(winningSlot, effectiveWheelSpinCount,               // Phase 1: ball released, both spin
            effectiveBallOrbitCount, effectiveMainSpinDuration);
        while (phase.MoveNext())
            yield return phase.Current;

        phase = CoastRoutine(winningSlot);                                             // Phase 2: wheel stopped, ball coasts
        while (phase.MoveNext())
            yield return phase.Current;

        phase = DropRoutine(winningSlot);                                              // Phase 3: ball drops into pocket
        while (phase.MoveNext())
            yield return phase.Current;

        phase = SettleRoutine();                                                       // Phase 4: ball settles in pocket
        while (phase.MoveNext())
            yield return phase.Current;
    }

    /// <summary>
    /// Phase 0: Wheel warmup — slight backspin, then accelerate forward.
    /// Ball stays static at its current position.
    /// </summary>
    private IEnumerator SpinRoutine()
    {
        float elapsed = 0f;
        float duration = GetPositiveDuration(wheelWarmupDuration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            float currentAngle;
            if (t < 0.2f)
            {
                // Backspin phase: 0 → wheelBackspinAngle degrees
                float bt = t / 0.2f;
                currentAngle = Mathf.Lerp(0f, wheelBackspinAngle, bt);
            }
            else
            {
                // Accelerate forward: wheelBackspinAngle → wheelWarmupForwardAngle
                float at = (t - 0.2f) / 0.8f;
                currentAngle = Mathf.Lerp(wheelBackspinAngle, wheelWarmupForwardAngle, at);
            }

            wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, currentAngle, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Phase 1: Main spin — ball is released near opposite side of target pocket.
    /// Wheel continues spinning (decelerating). Ball orbits counter-clockwise (decelerating).
    /// 
    /// Sub-phase 1a: Ball transitions from its scene position to the release position.
    /// Sub-phase 1b: Main orbital spin with wheel deceleration.
    /// </summary>
    private IEnumerator BallReleaseRoutine(RouletteSlot winningSlot,
        float effectiveWheelSpinCount, float effectiveBallOrbitCount, float effectiveMainSpinDuration)
    {
        // The wheel keeps spinning continuously through both sub-phases.
        // Sub-phase 1a adds extra rotation during ball reposition + wheel ramp-up.
        const float SUB1A_WHEEL_RAMP_ANGLE = 30f;

        float totalWheelAngle = 360f * effectiveWheelSpinCount;
        float startWheelAngle = wheelWarmupForwardAngle + SUB1A_WHEEL_RAMP_ANGLE;

        // Determine target pocket and release angle
        Transform targetPocket = GetPocketForSlot(winningSlot);
        Vector3 localTarget = wheelSpace.InverseTransformPoint(targetPocket.position);
        float targetAngle = Mathf.Atan2(localTarget.z, localTarget.x);
        float randomOffset = UnityEngine.Random.Range(-0.3f, 0.3f);
        float releaseAngle = targetAngle + Mathf.PI + randomOffset;

        // Read ball's current scene position
        Vector3 startLocalPos = ball.localPosition;
        float startOrbitRadius = new Vector3(startLocalPos.x, 0f, startLocalPos.z).magnitude;
        float startAngle = Mathf.Atan2(startLocalPos.z, startLocalPos.x);
        float startY = startLocalPos.y;

        // If ball is at origin, use default radius
        if (startOrbitRadius < 0.01f)
            startOrbitRadius = defaultOrbitRadius;

        float targetOrbitRadius = startOrbitRadius;

        // --- SUB-PHASE 1a: Transition ball from scene position to release position ---
        // Wheel continues rotating (gentle ramp from warmup into main spin).
        float positionElapsed = 0f;
        float positionDuration = GetPositiveDuration(ballPositionDuration);

        while (positionElapsed < positionDuration)
        {
            float t = positionElapsed / positionDuration;
            float curveValue = Mathf.SmoothStep(0f, 1f, t);

            // Angle: startAngle → releaseAngle (in radians)
            float currentAngle = Mathf.LerpAngle(startAngle * Mathf.Rad2Deg, releaseAngle * Mathf.Rad2Deg, curveValue) * Mathf.Deg2Rad;
            // Radius: startOrbitRadius → targetOrbitRadius
            float currentRadius = Mathf.Lerp(startOrbitRadius, targetOrbitRadius, curveValue);
            // Y: startY → spinHeight
            float currentY = Mathf.Lerp(startY, spinHeight, curveValue);

            ball.localPosition = new Vector3(
                Mathf.Cos(currentAngle) * currentRadius,
                currentY,
                Mathf.Sin(currentAngle) * currentRadius
            );

            // Wheel: gentle ramp from warmup angle toward main spin start
            float wheelRamp = Mathf.Lerp(0f, SUB1A_WHEEL_RAMP_ANGLE, curveValue);
            float curWheelAngle = wheelWarmupForwardAngle + wheelRamp;
            wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, curWheelAngle, 0f);

            positionElapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to exact release position
        ball.localPosition = new Vector3(
            Mathf.Cos(releaseAngle) * targetOrbitRadius,
            spinHeight,
            Mathf.Sin(releaseAngle) * targetOrbitRadius
        );

        // --- SUB-PHASE 1b: Main orbital spin ---
        float elapsed = 0f;
        float totalOrbitAngle = 360f * effectiveBallOrbitCount;

        float mainDuration = GetPositiveDuration(effectiveMainSpinDuration);

        while (elapsed < mainDuration)
        {
            float t = elapsed / mainDuration;

            // Wheel: continues from warmup, decelerating
            float wheelCurve = wheelSpinCurve.Evaluate(t);
            float wheelAngle = startWheelAngle + totalWheelAngle * wheelCurve;
            wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, wheelAngle, 0f);

            // Ball: orbiting counter-clockwise, decelerating (cumulative — framerate independent)
            float orbitProgress = ballOrbitCurve.Evaluate(t);
            float currentOrbitAngle = -totalOrbitAngle * orbitProgress;
            float orbitAngle = releaseAngle + currentOrbitAngle;

            // Radius wobble
            float wobble = 1f + orbitWobbleAmplitude * Mathf.Sin(orbitWobbleFrequency * elapsed);
            float currentRadius = targetOrbitRadius * wobble;

            ball.localPosition = new Vector3(
                Mathf.Cos(orbitAngle) * currentRadius,
                spinHeight,
                Mathf.Sin(orbitAngle) * currentRadius
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Finalize wheel
        float finalWheelAngle = startWheelAngle + totalWheelAngle;
        // The main spin already sets the wheel at this angle, but ensure precision snap
        wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, finalWheelAngle, 0f);
        // Note: wheel ends at wheelWarmupForwardAngle + SUB1A_WHEEL_RAMP_ANGLE + totalWheelAngle
    }

    /// <summary>
    /// Phase 2: Coast — wheel is stopped, ball continues orbiting alone.
    /// Uses progress-based Lerp to guarantee the ball reaches the exact pocket angle.
    /// </summary>
    private IEnumerator CoastRoutine(RouletteSlot winningSlot)
    {
        Transform targetPocket = GetPocketForSlot(winningSlot);
        Vector3 localTarget = wheelSpace.InverseTransformPoint(targetPocket.position);
        float targetAngle = Mathf.Atan2(localTarget.z, localTarget.x);

        // Current ball state
        Vector3 ballOffset = ball.localPosition;
        float orbitRadius = new Vector3(ballOffset.x, 0f, ballOffset.z).magnitude;
        float startAngle = Mathf.Atan2(ballOffset.z, ballOffset.x);

        // Calculate shortest counter-clockwise path to target
        float angleDiff = targetAngle - startAngle;
        while (angleDiff > 0f) angleDiff -= 2f * Mathf.PI;      // force negative (CCW)
        while (angleDiff < -2f * Mathf.PI) angleDiff += 2f * Mathf.PI; // max one full rotation

        float progress = 0f;
        float duration = GetPositiveDuration(coastDuration);

        while (progress < 1f)
        {
            // Quadratic ease-out: fast start, slow end
            float easedProgress = 1f - (1f - progress) * (1f - progress);

            // Guaranteed to reach exactly targetAngle at progress=1
            float currentAngle = startAngle + angleDiff * easedProgress;

            // Dampened wobble (reaches zero at end) — uses progress-based phase for pause/resume safety
            float wobblePhase = progress * duration * orbitWobbleFrequency;
            float wobble = 1f + orbitWobbleAmplitude * Mathf.Sin(wobblePhase) * (1f - progress);
            float currentRadius = orbitRadius * wobble;

            ball.localPosition = new Vector3(
                Mathf.Cos(currentAngle) * currentRadius,
                spinHeight,
                Mathf.Sin(currentAngle) * currentRadius
            );

            progress += Time.deltaTime / duration;
            yield return null;
        }

        // Final snap to exact pocket angle at spin height
        ball.localPosition = new Vector3(
            Mathf.Cos(targetAngle) * orbitRadius,
            spinHeight,
            Mathf.Sin(targetAngle) * orbitRadius
        );
    }

    /// <summary>
    /// Phase 3: Drop — ball descends from spinHeight to pocket Y with inward spiral.
    /// No bounce — smooth settle only.
    /// </summary>
    private IEnumerator DropRoutine(RouletteSlot winningSlot)
    {
        OnBallDropStarted?.Invoke();

        Transform targetPocket = GetPocketForSlot(winningSlot);
        Vector3 targetPosition = targetPocket.position + Vector3.up * landingYOffset;
        Vector3 localTarget = wheelSpace.InverseTransformPoint(targetPosition);

        // Current ball state in local space
        float startY = ball.localPosition.y;
        Vector3 ballOffset = ball.localPosition;
        float startRadius = new Vector3(ballOffset.x, 0f, ballOffset.z).magnitude;
        float targetRadius = new Vector3(localTarget.x, 0f, localTarget.z).magnitude;
        float startAngle = Mathf.Atan2(ballOffset.z, ballOffset.x);
        float targetAngle = Mathf.Atan2(localTarget.z, localTarget.x);

        float elapsed = 0f;
        float duration = GetPositiveDuration(dropDuration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curveValue = ballDropCurve.Evaluate(t);

            // Y: interpolate from current Y to target Y
            float currentY = Mathf.Lerp(startY, localTarget.y, curveValue);

            // Radius: interpolate from orbit radius to pocket radius (inward spiral)
            float currentRadius = Mathf.Lerp(startRadius, targetRadius, curveValue);

            // Angle: interpolate from current angle to target angle
            float currentAngle = Mathf.Lerp(startAngle, targetAngle, curveValue);

            ball.localPosition = new Vector3(
                Mathf.Cos(currentAngle) * currentRadius,
                currentY,
                Mathf.Sin(currentAngle) * currentRadius
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final snap in local space (matches local-space interpolation)
        ball.localPosition = localTarget;
    }

    /// <summary>
    /// Phase 4: Settle — small damped bounce/oscillation in the pocket.
    /// </summary>
    private IEnumerator SettleRoutine()
    {
        float elapsed = 0f;
        Vector3 startPos = ball.position;
        float bounceHeight = 0.02f;
        float duration = GetPositiveDuration(settleDuration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float heightOffset = Mathf.Sin(t * Mathf.PI * 3f) * bounceHeight * (1f - t);
            ball.position = startPos + Vector3.up * heightOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        ball.position = startPos;
    }

    private Transform GetPocketForSlot(RouletteSlot slot)
    {
        if (currentPocketTransforms == null || currentPocketTransforms.Length == 0)
            throw new InvalidOperationException("No roulette pocket transforms are available.");

        for (int i = 0; i < currentPocketTransforms.Length; i++)
        {
            string name = currentPocketTransforms[i].name;
            string numberPart = name.Substring(name.LastIndexOf('_') + 1);

            if (slot.IsDoubleZero && numberPart == "DoubleZero")
                return currentPocketTransforms[i];

            if (numberPart == slot.Id)
                return currentPocketTransforms[i];
        }

        Debug.LogWarning($"Pocket not found for slot {slot.Id}, returning first pocket");
        return currentPocketTransforms[0];
    }

    private bool CanPlaySpinVisual(RouletteSlot winningSlot)
    {
        if (winningSlot == null)
            return false;

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("RouletteWheelSpinAnimator: Animator is disabled.");
            return false;
        }

        if (wheelSpace == null || ball == null)
        {
            Debug.LogWarning("RouletteWheelSpinAnimator: Wheel visual references are missing.");
            return false;
        }

        if (currentPocketTransforms == null || currentPocketTransforms.Length == 0)
        {
            Debug.LogWarning("RouletteWheelSpinAnimator: Pocket references are missing.");
            return false;
        }

        return true;
    }

    private float CalculateExpectedSpinDuration(float effectiveMainSpinDuration)
    {
        return GetPositiveDuration(wheelWarmupDuration)
            + GetPositiveDuration(ballPositionDuration)
            + GetPositiveDuration(effectiveMainSpinDuration)
            + GetPositiveDuration(coastDuration)
            + GetPositiveDuration(dropDuration)
            + GetPositiveDuration(settleDuration);
    }

    private float GetPositiveDuration(float duration)
    {
        return Mathf.Max(0.01f, duration);
    }
}
