using System.Collections;
using UnityEngine;

public class CameraAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;

    [Header("Betting — Position & Rotation")]
    [SerializeField] private Vector3 bettingPosition = new Vector3(0f, 10f, 0f);
    [SerializeField] private Vector3 bettingRotation = new Vector3(90f, 0f, 0f);
    [SerializeField] private bool bettingIsOrthographic = true;
    [SerializeField] private float bettingOrthographicSize = 5f;

    [Header("Spinning — Position & Rotation")]
    [SerializeField] private Vector3 spinningPosition = new Vector3(0f, 2f, -5f);
    [SerializeField] private Vector3 spinningRotation = new Vector3(10f, 0f, 0f);
    [SerializeField] private bool spinningIsOrthographic = false;
    [SerializeField] private float spinningOrthographicSize = 5f;

    [Header("Animation")]
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float animationDuration = 0.5f;

    private Camera cam;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = GetComponentInChildren<Camera>();
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
            gameFlowController.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
            gameFlowController.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        if (gameFlowController != null &&
            gameFlowController.GameState.FlowState == GameFlowState.Betting)
        {
            SnapToPosition(bettingPosition, bettingRotation);
            ApplyProjection(bettingIsOrthographic, bettingOrthographicSize);
        }
    }

    private void HandleGameStateChanged()
    {
        if (gameFlowController == null || cam == null)
            return;

        GameFlowState state = gameFlowController.GameState.FlowState;

        if (state == GameFlowState.Betting)
        {
            StartAnimation(bettingPosition, Quaternion.Euler(bettingRotation));
            ApplyProjection(bettingIsOrthographic, bettingOrthographicSize);
        }
        else if (state == GameFlowState.Spinning)
        {
            StartAnimation(spinningPosition, Quaternion.Euler(spinningRotation));
            ApplyProjection(spinningIsOrthographic, spinningOrthographicSize);
        }
    }

    private void ApplyProjection(bool orthographic, float orthographicSize)
    {
        if (cam == null)
            return;

        cam.orthographic = orthographic;

        if (orthographic)
            cam.orthographicSize = orthographicSize;
    }

    private void SnapToPosition(Vector3 position, Vector3 eulerAngles)
    {
        cam.transform.SetPositionAndRotation(position, Quaternion.Euler(eulerAngles));
    }

    private void StartAnimation(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateToTarget(targetPosition, targetRotation));
    }

    private IEnumerator AnimateToTarget(Vector3 targetPosition, Quaternion targetRotation)
    {
        Transform ct = cam.transform;
        Vector3 startPosition = ct.position;
        Quaternion startRotation = ct.rotation;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            float curveValue = animationCurve.Evaluate(t);

            ct.position = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
            ct.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, curveValue);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ct.SetPositionAndRotation(targetPosition, targetRotation);

        animationCoroutine = null;
    }
}
