using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticsPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private SaveGameManager saveGameManager;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private RectTransform panelTransform;
    [SerializeField] private GameObject confirmRoot;
    [SerializeField] private TMP_Text overallStatsText;
    [SerializeField] private TMP_Text europeanStatsText;
    [SerializeField] private TMP_Text americanStatsText;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.18f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.96f, 0.96f, 0.96f);
    [SerializeField] private Vector3 shownScale = Vector3.one;
    [SerializeField] private AnimationCurve scaleInCurve = new AnimationCurve();
    [SerializeField] private AnimationCurve scaleOutCurve = new AnimationCurve();

    private readonly StringBuilder builder = new StringBuilder();
    private Coroutine animationCoroutine;
    private bool isPanelOpen;

    private void Awake()
    {
        ResolvePanelReferences();
        ValidateReferences();

        SetPanelHiddenImmediate();

        if (confirmRoot != null)
            confirmRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (resetButton != null)
            resetButton.onClick.AddListener(ShowConfirm);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmReset);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HideConfirm);

        if (gameFlowController != null)
        {
            gameFlowController.OnGameStateChanged += HandleGameStateChanged;
            gameFlowController.OnRoundResolved += HandleRoundResolved;
        }
    }

    private void OnDisable()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ShowConfirm);

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmReset);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(HideConfirm);

        if (gameFlowController != null)
        {
            gameFlowController.OnGameStateChanged -= HandleGameStateChanged;
            gameFlowController.OnRoundResolved -= HandleRoundResolved;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private void OpenPanel()
    {
        if (panelRoot == null || panelCanvasGroup == null || panelTransform == null)
            return;

        isPanelOpen = true;
        panelRoot.SetActive(true);
        HideConfirm();
        RefreshStatsText();

        StartPanelAnimation(true);
    }

    private void ClosePanel()
    {
        if (panelRoot == null || panelCanvasGroup == null || panelTransform == null)
            return;

        isPanelOpen = false;
        HideConfirm();

        StartPanelAnimation(false);
    }

    private void ShowConfirm()
    {
        if (confirmRoot == null)
            return;

        confirmRoot.SetActive(true);
    }

    private void HideConfirm()
    {
        if (confirmRoot == null)
            return;

        confirmRoot.SetActive(false);
    }

    private void ConfirmReset()
    {
        if (saveGameManager == null)
        {
            Debug.LogError("StatisticsPanelController: SaveGameManager reference is missing. Cannot reset data.");
            return;
        }

        saveGameManager.ResetDataAndRestartScene();
    }

    private void StartPanelAnimation(bool show)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimatePanel(show));
    }

    private void ResolvePanelReferences()
    {
        if (panelRoot == null)
            return;

        if (panelCanvasGroup == null || !panelCanvasGroup.transform.IsChildOf(panelRoot.transform))
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();

        if (panelTransform == null)
            panelTransform = panelRoot.GetComponent<RectTransform>();
    }

    private IEnumerator AnimatePanel(bool show)
    {
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        float startAlpha = panelCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;
        Vector3 startScale = panelTransform.localScale;
        Vector3 targetScale = show ? shownScale : hiddenScale;
        float duration = Mathf.Max(0.01f, animationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scaleT = show ? scaleInCurve.Evaluate(t) : scaleOutCurve.Evaluate(t);

            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            panelTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, scaleT);

            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        panelTransform.localScale = targetScale;
        panelCanvasGroup.interactable = show;
        panelCanvasGroup.blocksRaycasts = show;

        if (!show)
            panelRoot.SetActive(false);

        animationCoroutine = null;
    }

    private void SetPanelHiddenImmediate()
    {
        isPanelOpen = false;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (panelTransform != null)
            panelTransform.localScale = hiddenScale;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void HandleGameStateChanged()
    {
        if (isPanelOpen)
            RefreshStatsText();
    }

    private void HandleRoundResolved(RoundResult result)
    {
        if (isPanelOpen)
            RefreshStatsText();
    }

    private void RefreshStatsText()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
            return;

        PlayerStatistics overall = gameFlowController.StatisticsTracker.Statistics;
        PlayerStatistics european = gameFlowController.StatisticsTracker.EuropeanStatistics;
        PlayerStatistics american = gameFlowController.StatisticsTracker.AmericanStatistics;

        SetStatsText(overallStatsText, "Overall Stats", overall);
        SetStatsText(europeanStatsText, "European Stats", european);
        SetStatsText(americanStatsText, "American Stats", american);
    }

    private void SetStatsText(TMP_Text textField, string label, PlayerStatistics stats)
    {
        if (textField == null || stats == null)
            return;

        builder.Clear();
        builder.AppendLine($"<b>{label}</b>");
        builder.AppendLine($"Spins: {stats.TotalSpins}");
        builder.AppendLine($"Wins: {stats.TotalWins}");
        builder.AppendLine($"Losses: {stats.TotalLosses}");
        builder.AppendLine($"Win Rate: {stats.WinRate:0.0}%");
        builder.AppendLine($"Profit/Loss: {stats.TotalProfitLoss}");
        builder.AppendLine($"Total Wagered: {stats.TotalWagered}");
        builder.AppendLine($"Best Win: {stats.BestRoundNetProfit}");

        if (!string.IsNullOrEmpty(stats.LastWinningSlotId))
        {
            builder.AppendLine($"Last Slot: {stats.LastWinningSlotId}");
            builder.AppendLine($"Last Round Net: {stats.LastRoundNetProfit}");
        }

        textField.text = builder.ToString();
    }

    private void ValidateReferences()
    {
        if (gameFlowController == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a GameFlowController reference.");

        if (saveGameManager == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a SaveGameManager reference.");

        if (panelRoot == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a panel root reference.");

        if (panelCanvasGroup == null && (panelRoot == null || panelRoot.GetComponent<CanvasGroup>() == null))
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a panel CanvasGroup reference.");

        if (panelCanvasGroup != null && panelRoot != null && !panelCanvasGroup.transform.IsChildOf(panelRoot.transform))
            Debug.LogError($"{nameof(StatisticsPanelController)} panel CanvasGroup must belong to the statistics panel hierarchy.");

        if (panelTransform == null && (panelRoot == null || panelRoot.GetComponent<RectTransform>() == null))
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a panel RectTransform reference.");

        if (overallStatsText == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs an overall stats text reference.");

        if (europeanStatsText == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a european stats text reference.");

        if (americanStatsText == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs an american stats text reference.");

        if (openButton == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs an open button reference.");

        if (closeButton == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a close button reference.");

        if (resetButton == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a reset button reference.");

        if (confirmButton == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a confirm button reference.");

        if (cancelButton == null)
            Debug.LogError($"{nameof(StatisticsPanelController)} needs a cancel button reference.");
    }
}
