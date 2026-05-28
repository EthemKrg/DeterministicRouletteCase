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

    [Header("Wheel Motion")]
    [SerializeField] private float wheelStopDuration = 2.6f;
    [SerializeField] private float wheelTotalRotations = 4.5f;

    [Header("Ball Motion")]
    [SerializeField] private float ballAccelerationDuration = 1.0f;
    [SerializeField] private float ballDecelerationDuration = 2.4f;
    [SerializeField] private float ballFastSpeedDegPerSecond = 850f;
    [SerializeField] private float ballSlowSpeedDegPerSecond = 160f;
    [SerializeField] private float slowOrbitCountBeforeDrop = 1f;

    [Header("Drop Motion")]
    [SerializeField] private float dropDuration = 0.9f;
    [SerializeField] private float dropAngleSharpness = 1.35f;
    [SerializeField] private float dropRadiusStartDelay = 0.25f;
    [SerializeField] private float dropHeightStartDelay = 0.45f;
    [SerializeField] private float finalPocketSnapBlend = 0.35f;
    [SerializeField] private AnimationCurve dropRadiusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve dropHeightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Settle")]
    [SerializeField] private float settleDuration = 0.25f;
    [SerializeField] private float settleBounceHeight = 0.02f;

    [Header("Ball Placement")]
    [SerializeField] private float spinHeight = 2.2f;
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float landingYOffset = 0.15f;
    [SerializeField] private float orbitWobbleAmplitude = 0.02f;
    [SerializeField] private float orbitWobbleFrequency = 4f;

    [Header("Random Variation")]
    [SerializeField] private float wheelRotationVariation = 0.35f;
    [SerializeField] private float fastSpeedVariation = 70f;
    [SerializeField] private float durationVariation = 0.12f;

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
            spinCoroutine = StartCoroutine(SpinRoutine(winningSlot, plan));
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

    private IEnumerator SpinRoutine(RouletteSlot winningSlot, SpinPlan plan)
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
        ResetVisualState();

        float elapsed = 0f;
        bool dropEventSent = false;

        while (elapsed < plan.TotalDurationBeforeSettle)
        {
            float wheelAngle = EvaluateWheelAngle(elapsed, plan);
            wheelAngleDeg = wheelAngle;
            ApplyWheelRotation();

            if (elapsed < plan.DropStartTime)
            {
                float travel = EvaluateBallTravelBeforeDrop(elapsed, plan);
                ballAngleDeg = -travel;

                ApplyBallOrbit(ballAngleDeg, orbitRadius, spinHeight, elapsed);
            }
            else
            {
                if (!dropEventSent)
                {
                    dropEventSent = true;
                    OnBallDropStarted?.Invoke();
                }

                float dropElapsed = elapsed - plan.DropStartTime;
                ApplyDropMotion(winningSlot, plan, dropElapsed);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        wheelAngleDeg = plan.FinalWheelAngleDeg;
        ApplyWheelRotation();

        Transform targetPocket = GetPocketForSlot(winningSlot);
        ball.position = targetPocket.position + Vector3.up * landingYOffset;
        ballAngleDeg = plan.TargetBallAngleDeg;

        yield return SettleRoutine();

        spinCoroutine = null;
    }

    private SpinPlan CreateSpinPlan(RouletteSlot winningSlot)
    {
        orbitCenter = wheelSpace.position;

        float effectiveWheelRotations = Mathf.Max(
            0.1f,
            wheelTotalRotations + UnityEngine.Random.Range(-wheelRotationVariation, wheelRotationVariation));

        float effectiveWheelStopDuration = GetPositiveDuration(
            wheelStopDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));

        float effectiveBallAccelerationDuration = GetPositiveDuration(
            ballAccelerationDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));

        float effectiveBallDecelerationDuration = GetPositiveDuration(
            ballDecelerationDuration + UnityEngine.Random.Range(-durationVariation, durationVariation));

        float effectiveFastSpeed = Mathf.Max(
            ballSlowSpeedDegPerSecond + 30f,
            ballFastSpeedDegPerSecond + UnityEngine.Random.Range(-fastSpeedVariation, fastSpeedVariation));

        float effectiveSlowSpeed = Mathf.Max(20f, ballSlowSpeedDegPerSecond);
        float effectiveDropDuration = GetPositiveDuration(dropDuration);

        float finalWheelAngle = effectiveWheelRotations * 360f;
        float finalPocketAngle = GetPocketAngleForWheelAngle(winningSlot, finalWheelAngle);

        float accelerationTravel = GetAccelerationTravel(
            effectiveFastSpeed,
            effectiveBallAccelerationDuration,
            effectiveBallAccelerationDuration);

        float decelerationTravel = GetDecelerationTravel(
            effectiveFastSpeed,
            effectiveSlowSpeed,
            effectiveBallDecelerationDuration,
            effectiveBallDecelerationDuration);

        float dropTravel = GetDropTravel(effectiveSlowSpeed, effectiveDropDuration);

        float slowOrbitTravel = Mathf.Max(0f, slowOrbitCountBeforeDrop) * 360f;

        // During the drop, the ball keeps moving CCW and slows down to zero.
        // Therefore the drop must start this many degrees before the pocket.
        float dropStartWrappedAngle = NormalizeAngle360(finalPocketAngle + dropTravel);

        float travelBeforeAlignment = accelerationTravel + decelerationTravel;
        float targetTravelAtDropStart = GetTravelDistanceForWrappedAngle(dropStartWrappedAngle, travelBeforeAlignment);

        float alignmentTravel = targetTravelAtDropStart - travelBeforeAlignment;

        // The requested visible beat: slow speed, then one more full orbit, then drop.
        // Alignment is also slow speed, but it happens before that final readable orbit.
        float alignmentDuration = alignmentTravel / effectiveSlowSpeed;
        float slowOrbitDuration = slowOrbitTravel / effectiveSlowSpeed;

        float dropStartTime =
            effectiveBallAccelerationDuration +
            effectiveBallDecelerationDuration +
            alignmentDuration +
            slowOrbitDuration;

        float totalDurationBeforeSettle = dropStartTime + effectiveDropDuration;
        float totalDuration = totalDurationBeforeSettle + GetPositiveDuration(settleDuration);

        return new SpinPlan
        {
            WheelStopDuration = effectiveWheelStopDuration,
            FinalWheelAngleDeg = finalWheelAngle,

            BallAccelerationDuration = effectiveBallAccelerationDuration,
            BallDecelerationDuration = effectiveBallDecelerationDuration,
            BallFastSpeedDegPerSecond = effectiveFastSpeed,
            BallSlowSpeedDegPerSecond = effectiveSlowSpeed,

            AccelerationTravelDeg = accelerationTravel,
            DecelerationTravelDeg = decelerationTravel,
            AlignmentTravelDeg = alignmentTravel,
            AlignmentDuration = alignmentDuration,
            SlowOrbitTravelDeg = slowOrbitTravel,
            SlowOrbitDuration = slowOrbitDuration,

            DropDuration = effectiveDropDuration,
            DropTravelDeg = dropTravel,
            DropStartTime = dropStartTime,

            TargetPocketAngleDeg = finalPocketAngle,
            TargetBallAngleDeg = -targetTravelAtDropStart - slowOrbitTravel - dropTravel,

            TotalDurationBeforeSettle = totalDurationBeforeSettle,
            TotalDuration = totalDuration
        };
    }

    private void ResetVisualState()
    {
        orbitCenter = wheelSpace.position;

        wheelAngleDeg = 0f;
        ballAngleDeg = 0f;

        wheelSpace.localRotation = initialWheelRotation;
        ball.position = orbitCenter + OrbitOffset(0f, orbitRadius, spinHeight);
    }

    private float EvaluateWheelAngle(float elapsed, SpinPlan plan)
    {
        float t = Mathf.Clamp01(elapsed / plan.WheelStopDuration);
        float easedT = SmoothStep01(t);

        return plan.FinalWheelAngleDeg * easedT;
    }

    private float EvaluateBallTravelBeforeDrop(float elapsed, SpinPlan plan)
    {
        if (elapsed <= plan.BallAccelerationDuration)
        {
            return GetAccelerationTravel(
                plan.BallFastSpeedDegPerSecond,
                plan.BallAccelerationDuration,
                elapsed);
        }

        elapsed -= plan.BallAccelerationDuration;

        if (elapsed <= plan.BallDecelerationDuration)
        {
            return plan.AccelerationTravelDeg + GetDecelerationTravel(
                plan.BallFastSpeedDegPerSecond,
                plan.BallSlowSpeedDegPerSecond,
                plan.BallDecelerationDuration,
                elapsed);
        }

        elapsed -= plan.BallDecelerationDuration;

        if (elapsed <= plan.AlignmentDuration)
        {
            return plan.AccelerationTravelDeg
                + plan.DecelerationTravelDeg
                + plan.BallSlowSpeedDegPerSecond * elapsed;
        }

        elapsed -= plan.AlignmentDuration;

        return plan.AccelerationTravelDeg
            + plan.DecelerationTravelDeg
            + plan.AlignmentTravelDeg
            + plan.BallSlowSpeedDegPerSecond * Mathf.Min(elapsed, plan.SlowOrbitDuration);
    }

    private void ApplyDropMotion(RouletteSlot winningSlot, SpinPlan plan, float dropElapsed)
    {
        float t = Mathf.Clamp01(dropElapsed / plan.DropDuration);

        // Angle is intentionally a little sharper near the end.
        // The ball is already slow here, so a slightly firmer angular correction reads as
        // "falling into the pocket" instead of "sliding mathematically toward a point".
        float angleProgress = IntegratedDecelerationToStop01(t);
        angleProgress = Mathf.Pow(angleProgress, Mathf.Max(0.25f, dropAngleSharpness));

        // Radius and height should not collapse immediately.
        // First the ball keeps circling on the rim, then it tips inward and drops.
        float radiusT = Remap01(t, dropRadiusStartDelay, 1f);
        float heightT = Remap01(t, dropHeightStartDelay, 1f);

        float radiusProgress = EvaluateCurve01(dropRadiusCurve, radiusT, SmoothStep01(radiusT));
        float heightProgress = EvaluateCurve01(dropHeightCurve, heightT, SmoothStep01(heightT));

        float travelAtDropStart = plan.AccelerationTravelDeg
            + plan.DecelerationTravelDeg
            + plan.AlignmentTravelDeg
            + plan.SlowOrbitTravelDeg;

        ballAngleDeg = -(travelAtDropStart + plan.DropTravelDeg * angleProgress);

        Transform targetPocket = GetPocketForSlot(winningSlot);
        Vector3 targetWorldPos = targetPocket.position + Vector3.up * landingYOffset;

        Vector3 targetOffset = targetWorldPos - orbitCenter;
        float targetRadius = new Vector3(targetOffset.x, 0f, targetOffset.z).magnitude;
        float targetHeight = targetOffset.y;

        float currentRadius = Mathf.Lerp(orbitRadius, targetRadius, radiusProgress);
        float currentHeight = Mathf.Lerp(spinHeight, targetHeight, heightProgress);

        Vector3 orbitPosition = orbitCenter + OrbitOffset(ballAngleDeg * Mathf.Deg2Rad, currentRadius, currentHeight);

        // In the final slice, blend to the exact pocket transform.
        // This hides tiny angle/reference mismatches from the wheel mesh and makes the ball
        // visibly commit to the selected slot.
        float snapT = Remap01(t, 1f - Mathf.Clamp01(finalPocketSnapBlend), 1f);
        snapT = SmoothStep01(snapT);

        ball.position = Vector3.Lerp(orbitPosition, targetWorldPos, snapT);
    }

    private IEnumerator SettleRoutine()
    {
        Vector3 startPos = ball.position;
        float elapsed = 0f;
        float duration = GetPositiveDuration(settleDuration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float heightOffset = Mathf.Sin(t * Mathf.PI * 3f) * settleBounceHeight * (1f - t);

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

        Debug.LogWarning($"RouletteWheelSpinAnimator: Pocket not found for slot {slot.Id}. Returning first pocket.");
        return currentPocketTransforms[0];
    }

    private float GetPocketAngleForWheelAngle(RouletteSlot slot, float finalWheelAngleDeg)
    {
        Quaternion previousRotation = wheelSpace.localRotation;

        wheelSpace.localRotation = initialWheelRotation * Quaternion.Euler(0f, finalWheelAngleDeg, 0f);

        float angle = GetPocketAngleDeg(slot);

        wheelSpace.localRotation = previousRotation;

        return angle;
    }

    private float GetPocketAngleDeg(RouletteSlot slot)
    {
        Transform targetPocket = GetPocketForSlot(slot);
        Vector3 targetWorldPos = targetPocket.position + Vector3.up * landingYOffset;
        Vector3 targetOffset = targetWorldPos - orbitCenter;

        return Mathf.Atan2(targetOffset.z, targetOffset.x) * Mathf.Rad2Deg;
    }

    private float GetTravelDistanceForWrappedAngle(float targetWrappedAngle, float minimumTravel)
    {
        // Ball angle is -travel because it moves counter-clockwise.
        float baseTravel = NormalizeAngle360(-targetWrappedAngle);

        while (baseTravel < minimumTravel)
            baseTravel += 360f;

        return baseTravel;
    }

    private float GetAccelerationTravel(float fastSpeed, float duration, float elapsed)
    {
        float safeDuration = GetPositiveDuration(duration);
        float t = Mathf.Clamp01(elapsed / safeDuration);

        // Integral of SmoothStep speed from 0 to fast speed.
        return fastSpeed * safeDuration * IntegratedSmoothStep01(t);
    }

    private float GetDecelerationTravel(float fastSpeed, float slowSpeed, float duration, float elapsed)
    {
        float safeDuration = GetPositiveDuration(duration);
        float clampedElapsed = Mathf.Clamp(elapsed, 0f, safeDuration);
        float t = clampedElapsed / safeDuration;

        // Speed is Lerp(fast, slow, SmoothStep(t)).
        float travel = fastSpeed * clampedElapsed
            + (slowSpeed - fastSpeed) * safeDuration * IntegratedSmoothStep01(t);

        if (elapsed > safeDuration)
            travel += slowSpeed * (elapsed - safeDuration);

        return travel;
    }

    private float GetDropTravel(float slowSpeed, float duration)
    {
        // Drop starts at slow speed and ends at zero.
        // Integrated distance is half of speed * time.
        return Mathf.Max(1f, slowSpeed * GetPositiveDuration(duration) * 0.5f);
    }

    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private float IntegratedSmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);

        // Integral of 3t^2 - 2t^3.
        // At t = 1, result is 0.5.
        return t * t * t - 0.5f * t * t * t * t;
    }

    private float IntegratedDecelerationToStop01(float t)
    {
        t = Mathf.Clamp01(t);

        // Normalized integral of speed = 1 - SmoothStep(t).
        // Starts with non-zero speed and ends with zero speed.
        return 2f * t - 2f * t * t * t + t * t * t * t;
    }

    private float Remap01(float value, float from, float to)
    {
        if (Mathf.Approximately(from, to))
            return value >= to ? 1f : 0f;

        return Mathf.Clamp01((value - from) / (to - from));
    }

    private float EvaluateCurve01(AnimationCurve curve, float t, float fallback)
    {
        t = Mathf.Clamp01(t);

        if (curve == null || curve.length == 0)
            return fallback;

        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private float NormalizeAngle360(float angleDeg)
    {
        angleDeg %= 360f;

        if (angleDeg < 0f)
            angleDeg += 360f;

        return angleDeg;
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
        public float WheelStopDuration;
        public float FinalWheelAngleDeg;

        public float BallAccelerationDuration;
        public float BallDecelerationDuration;
        public float BallFastSpeedDegPerSecond;
        public float BallSlowSpeedDegPerSecond;

        public float AccelerationTravelDeg;
        public float DecelerationTravelDeg;
        public float AlignmentTravelDeg;
        public float AlignmentDuration;
        public float SlowOrbitTravelDeg;
        public float SlowOrbitDuration;

        public float DropDuration;
        public float DropTravelDeg;
        public float DropStartTime;

        public float TargetPocketAngleDeg;
        public float TargetBallAngleDeg;

        public float TotalDurationBeforeSettle;
        public float TotalDuration;
    }
}
