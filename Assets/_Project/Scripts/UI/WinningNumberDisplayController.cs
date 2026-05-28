using System.Collections;
using TMPro;
using UnityEngine;

public class WinningNumberDisplayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private TMP_Text winningNumberText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform backgroundPanel;
    [SerializeField] private ParticleSystem confettiParticleSystem;

    [Header("Slide Animation")]
    [SerializeField] private float slideOffsetY = 100f;
    [SerializeField] private AnimationCurve slideInCurve = new AnimationCurve();
    [SerializeField] private AnimationCurve slideOutCurve = new AnimationCurve();
    [SerializeField] private float slideDuration = 0.4f;

    [Header("Text Scale Bounce")]
    [SerializeField] private AnimationCurve scaleCurve = new AnimationCurve();
    [SerializeField] private float scaleDuration = 0.5f;

    [Header("Background Panel Scale")]
    [SerializeField] private AnimationCurve bgScaleInCurve = new AnimationCurve();
    [SerializeField] private AnimationCurve bgScaleOutCurve = new AnimationCurve();
    [SerializeField] private float bgScaleDuration = 0.4f;

    [Header("Display & Fade")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine displayCoroutine;
    private Vector2 originalAnchoredPosition;
    private bool hasValidReferences;

    private void Awake()
    {
        if (winningNumberText == null || canvasGroup == null || backgroundPanel == null)
        {
            hasValidReferences = false;
            return;
        }

        hasValidReferences = true;

        originalAnchoredPosition = backgroundPanel.anchoredPosition;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        winningNumberText.gameObject.SetActive(false);

        backgroundPanel.localScale = Vector3.zero;
        backgroundPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
            gameFlowController.OnRoundResolved += HandleRoundResolved;
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
            gameFlowController.OnRoundResolved -= HandleRoundResolved;
    }

    private void HandleRoundResolved(RoundResult result)
    {
        if (result == null || result.WinningSlot == null)
            return;

        if (!result.HasAnyWinningBet)
            return;

        ShowWinningNumber(result.WinningSlot);

        if (confettiParticleSystem != null)
            confettiParticleSystem.Play();
    }

    private void ShowWinningNumber(RouletteSlot winningSlot)
    {
        if (!hasValidReferences)
            return;

        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        winningNumberText.text = winningSlot.IsDoubleZero ? "00" : winningSlot.Id;
        winningNumberText.gameObject.SetActive(true);
        winningNumberText.transform.localScale = Vector3.zero;

        Vector2 startPos = originalAnchoredPosition;
        startPos.y += slideOffsetY;
        backgroundPanel.anchoredPosition = startPos;
        backgroundPanel.localScale = Vector3.zero;
        backgroundPanel.gameObject.SetActive(true);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        displayCoroutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        float phaseMax = Mathf.Max(slideDuration, scaleDuration, bgScaleDuration);
        float elapsed = 0f;

        while (elapsed < phaseMax)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / slideDuration);
            Vector2 pos = originalAnchoredPosition;
            pos.y += Mathf.Lerp(slideOffsetY, 0f, slideInCurve.Evaluate(t));
            backgroundPanel.anchoredPosition = pos;

            t = Mathf.Clamp01(elapsed / scaleDuration);
            winningNumberText.transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

            t = Mathf.Clamp01(elapsed / bgScaleDuration);
            backgroundPanel.localScale = Vector3.one * bgScaleInCurve.Evaluate(t);

            yield return null;
        }

        backgroundPanel.anchoredPosition = originalAnchoredPosition;
        backgroundPanel.localScale = Vector3.one;
        winningNumberText.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(displayDuration);

        yield return AnimateOut();
        displayCoroutine = null;
    }

    private IEnumerator AnimateOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeOutDuration);

            Vector2 pos = originalAnchoredPosition;
            pos.y += Mathf.Lerp(slideOffsetY, 0f, slideOutCurve.Evaluate(t));
            backgroundPanel.anchoredPosition = pos;

            backgroundPanel.localScale = Vector3.one * bgScaleOutCurve.Evaluate(t);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        winningNumberText.gameObject.SetActive(false);

        backgroundPanel.gameObject.SetActive(false);
        backgroundPanel.anchoredPosition = originalAnchoredPosition;
    }
}
