using System.Collections;
using UnityEngine;

public abstract class Selectable3DButton : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;

    [Header("Press Animation")]
    [SerializeField] private float pressScaleMultiplier = 1.12f;
    [SerializeField] private float pressScaleUpDuration = 0.08f;
    [SerializeField] private float pressScaleDownDuration = 0.12f;

    private Vector3 defaultLocalScale;
    private Coroutine pressCoroutine;

    protected virtual void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        defaultLocalScale = visualRoot.localScale;
    }

    /// <summary>
    /// Toggle selected state (used by chip selection).
    /// Table buttons do not use this method.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (visualRoot == null)
            return;

        visualRoot.localScale = selected
            ? defaultLocalScale * pressScaleMultiplier
            : defaultLocalScale;
    }

    public void ResetVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localScale = defaultLocalScale;
    }

    /// <summary>
    /// Plays an instant press animation: scale up then scale down.
    /// Used by table game controls buttons.
    /// </summary>
    public virtual void PlayPressAnimation()
    {
        if (pressCoroutine != null)
            StopCoroutine(pressCoroutine);

        pressCoroutine = StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        Vector3 targetScale = defaultLocalScale * pressScaleMultiplier;

        // Scale up
        float elapsed = 0f;
        while (elapsed < pressScaleUpDuration)
        {
            elapsed += Time.deltaTime;
            visualRoot.localScale = Vector3.Lerp(defaultLocalScale, targetScale, elapsed / pressScaleUpDuration);
            yield return null;
        }

        visualRoot.localScale = targetScale;

        // Scale down
        elapsed = 0f;
        while (elapsed < pressScaleDownDuration)
        {
            elapsed += Time.deltaTime;
            visualRoot.localScale = Vector3.Lerp(targetScale, defaultLocalScale, elapsed / pressScaleDownDuration);
            yield return null;
        }

        visualRoot.localScale = defaultLocalScale;
        pressCoroutine = null;
    }
}
