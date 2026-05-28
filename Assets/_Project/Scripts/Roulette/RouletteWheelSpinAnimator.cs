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

    [Header("Phase 1: Accelerate")]
    [SerializeField] private float accelerationDuration = 1.25f;
    [SerializeField] private float wheelAccelerationRotations = 2.75f;
    [SerializeField] private float ballFastSpeedDegPerSecond = 850f;

    [Header("Phase 2: Decelerate To Slow Speed")]
    [SerializeField] private float wheelDecelerationDuration = 2.4f;
    [SerializeField] private float wheelDecelerationRotations = 2.25f;
    [SerializeField] private float ballDecelerationDuration = 2.8f;
    [SerializeField] private float ballSlowSpeedDegPerSecond = 120f;

    [Header("Phase 3: Slow Orbit Before Drop")]
    [SerializeField] private float slowOrbitCount = 1f;

    [Header("Phase 4: Approach / Drop")]
    [SerializeField] private float approachDuration = 1.15f;
    [SerializeField] private AnimationCurve approachRadiusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve approachHeightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Phase 5: Settle")]
    [SerializeField] private float settleDuration = 0.3f;

    [Header("Ball Settings")]
    [SerializeField] private float spinHeight = 2.2f;
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float landingYOffset = 0.15f;
    [SerializeField] private float orbitWobbleAmplitude = 0.025f;
    [SerializeField] private float orbitWobbleFrequency = 4f;

    [Header("Random Variation")]
    [SerializeField] private float fastSpeedVariation = 80f;
    [SerializeField] private float durationVariation = 0.15f;

    public event Action OnBallDropStarted;

    public bool IsSpinAnimationPlaying => spinCoroutine != null;

    private float currentSpinDuration;
    public float ExpectedSpinDurationSeconds => currentSpinDuration;

    private Transform[] euPocketTransforms;
    private Transform[] usPocketTransforms;
    private Transform[] currentPocketTransforms;

    private Quaternion initialWheelRotation;
    private Coroutine spinCoroutine;

    private Vector3 orbitCenter;
    private float wheelAngleDeg;
    private float ballAngleDeg;

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
        StopSpinAnimation();

        if (!CanPlaySpinVisual(winningSlot))
            return 0f;

        SpinPlan plan = CreateSpinPlan(winningSlot);
        currentSpinDuration = plan.TotalDuration;

        try
        {
            spinCoroutine = StartCoroutine(SpinAnimationRoutine(winningSlot, plan));
        }
        catch (Exception exception)
        {
            Debug.LogError($"RouletteWheelSpinAnimator: Spin could not start: {exception.Message}");
            spinCoroutine = null;
            return 0f;
        }

        return currentSpinDuration;
    }

    public void StopSpinAnimation()
    {
        if (spinCoroutine == null)
            return;

        StopCoroutine(spinCoroutine);
        spinCoroutine = null;
    }

    private IEnumerator SpinAnimationRoutine(RouletteSlot winningSlot, SpinPlan plan)
    {
        IEnumerator routine = AnimateSpin(winningSlot, plan);

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
                Debug.LogError($"RouletteWheelSpinAnimator: Spin failed: {exception.Message}");
                break;
            }

            if (!hasNext)
                break;

            yield return current;
        }

        spinCoroutine = null;
    }

    private IEnumerator AnimateSpin(RouletteSlot winningSlot, SpinPlan plan)
    {
        PrepareSpinStart();

        yield return AccelerateRoutine(plan);
        yield return DecelerateRoutine(plan);
        yield return AlignToApproachStartRoutine(plan);
        yield return SlowOrbitRoutine(plan);

        OnBallDropStarted?.Invoke();

        yield return ApproachPocketRoutine(winningSlot, plan);
        yield return SettleRoutine();
    }

    private void PrepareSpinStart()
    {
        orbitCenter = wheelSpace.position;

        wheelAngleDeg = 0f;
        ballAngleDeg = 0f;

        wheelSpace.localRotation = initialWheelRotation;
        ball.position = orbitCenter + OrbitOffset(0f, orbitRadius, spinHeight);
    }

    private SpinPlan CreateSpinPlan(RouletteSlot winningSlot)
    {
        orbitCenter = wheelSpace.position;

        float effectiveAccelerationDuration = GetPositiveDuration(
            accelerationDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));

        float effectiveWheelDecelerationDuration = GetPositiveDuration(
            wheelDecelerationDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));

        float effectiveBallDecelerationDuration = GetPositiveDuration(
            ballDecelerationDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));

        float effectiveFastSpeed = Mathf.Max(
            ballSlowSpeedDegPerSecond + 30f,
            ballFastSpeedDegPerSecond + UnityEngine.Random.Range(-fastSpeedVariation, fastSpeedVariation));

        float effectiveSlowSpeed = Mathf.Max(20f, ballSlowSpeedDegPerSecond);

        float finalWheelAngle = wheelAccelerationRotations * 360f + wheelDecelerationRotations * 360f;
        float finalPocketAngle = GetPocketAngleForWheelAngle(winningSlot, finalWheelAngle);

        float approachTravelAngle = GetApproachTravelAngle(effectiveSlowSpeed, approachDuration);
        float approachStartWrappedAngle = NormalizeAngle360(finalPocketAngle + approachTravelAngle);

        float accelTravelAngle = GetAccelerationTravelAngle(effectiveFastSpeed, effectiveAccelerationDuration);

        float decelerationPhaseDuration = Mathf.Max(effectiveWheelDecelerationDuration, effectiveBallDecelerationDuration);
        float decelTravelAngle = GetDecelerationTravelAngle(
            effectiveFastSpeed,
            effectiveSlowSpeed,
            effectiveBallDecelerationDuration,
            decelerationPhaseDuration);

        float angleAfterDeceleration = -(accelTravelAngle + decelTravelAngle);

        float alignmentTravelAngle = GetCcwDistanceToWrappedAngle(angleAfterDeceleration, approachStartWrappedAngle);
        if (alignmentTravelAngle < 0.1f)
            alignmentTravelAngle = 0f;

        float alignmentDuration = alignmentTravelAngle / effectiveSlowSpeed;
        float slowOrbitDuration = GetSlowOrbitDuration(effectiveSlowSpeed);

        float totalDuration =
            effectiveAccelerationDuration +
            decelerationPhaseDuration +
            alignmentDuration +
            slowOrbitDuration +
            GetPositiveDuration(approachDuration) +
            GetPositiveDuration(settleDuration);

        return new SpinPlan
        {
            AccelerationDuration = effectiveAccelerationDuration,
            WheelDecelerationDuration = effectiveWheelDecelerationDuration,
            BallDecelerationDuration = effectiveBallDecelerationDuration,
            DecelerationPhaseDuration = decelerationPhaseDuration,
            BallFastSpeedDegPerSecond = effectiveFastSpeed,
            BallSlowSpeedDegPerSecond = effectiveSlowSpeed,
            FinalWheelAngleDeg = finalWheelAngle,
            ApproachTravelAngleDeg = approachTravelAngle,
            ApproachStartWrappedAngleDeg = approachStartWrappedAngle,
            AlignmentTravelAngleDeg = alignmentTravelAngle,
            AlignmentDuration = alignmentDuration,
            SlowOrbitDuration = slowOrbitDuration,
            TotalDuration = totalDuration
        };
    }

    private IEnumerator AccelerateRoutine(SpinPlan plan)
    {
        float startWheelAngle = wheelAngleDeg;
        float targetWheelAngle = startWheelAngle + wheelAccelerationRotations * 360f;

        float startBallAngle = ballAngleDeg;

        float elapsed = 0f;

        while (elapsed < plan.AccelerationDuration)
        {
            float t = elapsed / plan.AccelerationDuration;
            float smoothT = SmoothStep01(t);

            wheelAngleDeg = Mathf.LerpUnclamped(startWheelAngle, targetWheelAngle, smoothT);

            float ballTravel = GetAccelerationTravelAngle(
                plan.BallFastSpeedDegPerSecond,
                plan.AccelerationDuration,
                t);

            ballAngleDeg = startBallAngle - ballTravel;

            ApplyWheelRotation();
            ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, elapsed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        wheelAngleDeg = targetWheelAngle;
        ballAngleDeg = startBallAngle - GetAccelerationTravelAngle(
            plan.BallFastSpeedDegPerSecond,
            plan.AccelerationDuration);

        ApplyWheelRotation();
        ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, plan.AccelerationDuration);
    }

    private IEnumerator DecelerateRoutine(SpinPlan plan)
    {
        float startWheelAngle = wheelAngleDeg;
        float targetWheelAngle = plan.FinalWheelAngleDeg;

        float startBallAngle = ballAngleDeg;

        float fullBallRampTravel = GetDecelerationTravelAngle(
            plan.BallFastSpeedDegPerSecond,
            plan.BallSlowSpeedDegPerSecond,
            plan.BallDecelerationDuration,
            plan.BallDecelerationDuration);

        float elapsed = 0f;

        while (elapsed < plan.DecelerationPhaseDuration)
        {
            float wheelT = Mathf.Clamp01(elapsed / plan.WheelDecelerationDuration);
            float wheelProgress = SmoothStep01(wheelT);
            wheelAngleDeg = Mathf.LerpUnclamped(startWheelAngle, targetWheelAngle, wheelProgress);

            float ballTravel;

            if (elapsed <= plan.BallDecelerationDuration)
            {
                ballTravel = GetDecelerationTravelAngle(
                    plan.BallFastSpeedDegPerSecond,
                    plan.BallSlowSpeedDegPerSecond,
                    plan.BallDecelerationDuration,
                    elapsed);
            }
            else
            {
                ballTravel = fullBallRampTravel
                    + plan.BallSlowSpeedDegPerSecond * (elapsed - plan.BallDecelerationDuration);
            }

            ballAngleDeg = startBallAngle - ballTravel;

            ApplyWheelRotation();
            ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, elapsed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        wheelAngleDeg = targetWheelAngle;

        float finalBallTravel = GetDecelerationTravelAngle(
            plan.BallFastSpeedDegPerSecond,
            plan.BallSlowSpeedDegPerSecond,
            plan.BallDecelerationDuration,
            plan.DecelerationPhaseDuration);

        ballAngleDeg = startBallAngle - finalBallTravel;

        ApplyWheelRotation();
        ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, plan.DecelerationPhaseDuration);
    }

    private IEnumerator AlignToApproachStartRoutine(SpinPlan plan)
    {
        if (plan.AlignmentTravelAngleDeg <= 0.01f)
            yield break;

        float startAngle = ballAngleDeg;
        float targetAngle = startAngle - plan.AlignmentTravelAngleDeg;

        float elapsed = 0f;

        while (elapsed < plan.AlignmentDuration)
        {
            float ballTravel = plan.BallSlowSpeedDegPerSecond * elapsed;
            ballAngleDeg = startAngle - ballTravel;

            ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, elapsed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ballAngleDeg = targetAngle;
        ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, plan.AlignmentDuration);
    }

    private IEnumerator SlowOrbitRoutine(SpinPlan plan)
    {
        float startAngle = ballAngleDeg;
        float targetAngle = startAngle - Mathf.Max(0f, slowOrbitCount) * 360f;

        float elapsed = 0f;

        while (elapsed < plan.SlowOrbitDuration)
        {
            float ballTravel = plan.BallSlowSpeedDegPerSecond * elapsed;
            ballAngleDeg = startAngle - ballTravel;

            ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, elapsed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ballAngleDeg = targetAngle;
        ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, plan.SlowOrbitDuration);
    }

    private IEnumerator ApproachPocketRoutine(RouletteSlot winningSlot, SpinPlan plan)
    {
        Transform targetPocket = GetPocketForSlot(winningSlot);
        Vector3 targetWorldPos = targetPocket.position + Vector3.up * landingYOffset;

        Vector3 targetOffset = targetWorldPos - orbitCenter;
        float targetRadius = new Vector3(targetOffset.x, 0f, targetOffset.z).magnitude;
        float targetHeight = targetOffset.y;

        Vector3 currentOffset = ball.position - orbitCenter;
        float startRadius = new Vector3(currentOffset.x, 0f, currentOffset.z).magnitude;
        float startHeight = currentOffset.y;

        float startAngle = ballAngleDeg;
        float targetAngle = startAngle - plan.ApproachTravelAngleDeg;

        float duration = GetPositiveDuration(approachDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // This progress is the integral of a speed that starts at slow speed and eases to zero.
            // It avoids the common "slow orbit, then sudden approach acceleration" problem.
            float angleProgress = IntegratedDecelerationToStop01(t);

            float radiusProgress = EvaluateCurve01(approachRadiusCurve, t, SmoothStep01(t));
            float heightProgress = EvaluateCurve01(approachHeightCurve, t, SmoothStep01(t));

            float currentAngle = Mathf.LerpUnclamped(startAngle, targetAngle, angleProgress);
            float currentRadius = Mathf.Lerp(startRadius, targetRadius, radiusProgress);
            float currentHeight = Mathf.Lerp(startHeight, targetHeight, heightProgress);

            ball.position = orbitCenter + OrbitOffset(currentAngle * Mathf.Deg2Rad, currentRadius, currentHeight);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ballAngleDeg = targetAngle;
        ball.position = targetWorldPos;
    }

    private IEnumerator SettleRoutine()
    {
        Vector3 startPos = ball.position;
        float bounceHeight = 0.02f;

        float duration = GetPositiveDuration(settleDuration);
        float elapsed = 0f;

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

    private void ApplyWheelRotation()
    {
        wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, wheelAngleDeg, 0f);
    }

    private void ApplyBallOrbit(float angleDeg, float radius, float height, float elapsed)
    {
        float wobble = 1f + orbitWobbleAmplitude * Mathf.Sin(orbitWobbleFrequency * elapsed);
        float currentRadius = radius * wobble;

        ball.position = orbitCenter + OrbitOffset(angleDeg * Mathf.Deg2Rad, currentRadius, height);
    }

    private Vector3 OrbitOffset(float angleRad, float radius, float height)
    {
        return new Vector3(
            Mathf.Cos(angleRad) * radius,
            height,
            Mathf.Sin(angleRad) * radius);
    }

    private Transform GetPocketForSlot(RouletteSlot slot)
    {
        if (currentPocketTransforms == null || currentPocketTransforms.Length == 0)
            throw new InvalidOperationException("No roulette pocket transforms are available.");

        for (int i = 0; i < currentPocketTransforms.Length; i++)
        {
            string pocketName = currentPocketTransforms[i].name;
            int separatorIndex = pocketName.LastIndexOf('_');

            if (separatorIndex < 0 || separatorIndex >= pocketName.Length - 1)
                continue;

            string numberPart = pocketName.Substring(separatorIndex + 1);

            if (slot.IsDoubleZero && numberPart == "DoubleZero")
                return currentPocketTransforms[i];

            if (numberPart == slot.Id)
                return currentPocketTransforms[i];
        }

        Debug.LogWarning($"Pocket not found for slot {slot.Id}, returning first pocket.");
        return currentPocketTransforms[0];
    }

    private float GetPocketAngleForWheelAngle(RouletteSlot slot, float finalWheelAngleDeg)
    {
        Quaternion originalRotation = wheelSpace.localRotation;

        wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, finalWheelAngleDeg, 0f);

        float angle = GetPocketAngleDeg(slot);

        wheelSpace.localRotation = originalRotation;

        return angle;
    }

    private float GetPocketAngleDeg(RouletteSlot slot)
    {
        Transform targetPocket = GetPocketForSlot(slot);
        Vector3 targetWorldPos = targetPocket.position + Vector3.up * landingYOffset;
        Vector3 targetOffset = targetWorldPos - orbitCenter;

        return Mathf.Atan2(targetOffset.z, targetOffset.x) * Mathf.Rad2Deg;
    }

    private float GetAccelerationTravelAngle(float fastSpeed, float duration)
    {
        return fastSpeed * duration * 0.5f;
    }

    private float GetAccelerationTravelAngle(float fastSpeed, float duration, float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);

        // Integral of SmoothStep01(t): t^3 - 0.5t^4
        // The final value at t=1 is 0.5, so travel = fastSpeed * duration * 0.5.
        return fastSpeed * duration * (t * t * t - 0.5f * t * t * t * t);
    }

    private float GetDecelerationTravelAngle(float fastSpeed, float slowSpeed, float rampDuration, float elapsed)
    {
        float duration = GetPositiveDuration(rampDuration);
        float clampedElapsed = Mathf.Clamp(elapsed, 0f, duration);
        float t = clampedElapsed / duration;

        // Speed curve:
        // speed = Lerp(fastSpeed, slowSpeed, SmoothStep01(t))
        //
        // Integrated travel:
        // fast * time + (slow - fast) * duration * Integral(SmoothStep)
        float integralSmoothStep = t * t * t - 0.5f * t * t * t * t;

        float travel = fastSpeed * clampedElapsed
            + (slowSpeed - fastSpeed) * duration * integralSmoothStep;

        if (elapsed > duration)
            travel += slowSpeed * (elapsed - duration);

        return travel;
    }

    private float GetApproachTravelAngle(float slowSpeed, float duration)
    {
        // Decelerating from slowSpeed to zero over approachDuration travels half of speed * time.
        return Mathf.Max(1f, slowSpeed * GetPositiveDuration(duration) * 0.5f);
    }

    private float GetSlowOrbitDuration(float slowSpeed)
    {
        float speed = Mathf.Max(1f, slowSpeed);
        float angle = Mathf.Max(0f, slowOrbitCount) * 360f;

        return Mathf.Max(0.01f, angle / speed);
    }

    private float GetCcwDistanceToWrappedAngle(float fromUnwrappedDeg, float toWrappedDeg)
    {
        float fromWrapped = NormalizeAngle360(fromUnwrappedDeg);
        float targetWrapped = NormalizeAngle360(toWrappedDeg);

        return NormalizeAngle360(fromWrapped - targetWrapped);
    }

    private float NormalizeAngle360(float angleDeg)
    {
        angleDeg %= 360f;

        if (angleDeg < 0f)
            angleDeg += 360f;

        return angleDeg;
    }

    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private float IntegratedDecelerationToStop01(float t)
    {
        t = Mathf.Clamp01(t);

        // Normalized integral of speed = 1 - SmoothStep01(t).
        // Starts with non-zero velocity and ends with zero velocity.
        return 2f * t - 2f * t * t * t + t * t * t * t;
    }

    private float EvaluateCurve01(AnimationCurve curve, float t, float fallback)
    {
        t = Mathf.Clamp01(t);

        if (curve == null || curve.length == 0)
            return fallback;

        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private float GetPositiveDuration(float duration)
    {
        return Mathf.Max(0.01f, duration);
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

    private struct SpinPlan
    {
        public float AccelerationDuration;
        public float WheelDecelerationDuration;
        public float BallDecelerationDuration;
        public float DecelerationPhaseDuration;
        public float BallFastSpeedDegPerSecond;
        public float BallSlowSpeedDegPerSecond;
        public float FinalWheelAngleDeg;
        public float ApproachTravelAngleDeg;
        public float ApproachStartWrappedAngleDeg;
        public float AlignmentTravelAngleDeg;
        public float AlignmentDuration;
        public float SlowOrbitDuration;
        public float TotalDuration;
    }
}
