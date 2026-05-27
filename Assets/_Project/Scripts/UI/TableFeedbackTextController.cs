using TMPro;
using UnityEngine;

public class TableFeedbackTextController : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private TMP_Text feedbackText;

    private void Awake()
    {
        if (feedbackText == null)
            feedbackText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnFeedbackRequested += ShowFeedback;
        gameFlowController.OnRoundResolved += HandleRoundResolved;
    }

    private void OnDisable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnFeedbackRequested -= ShowFeedback;
        gameFlowController.OnRoundResolved -= HandleRoundResolved;
    }

    private void HandleRoundResolved(RoundResult result)
    {
        ShowFeedback(result.HasAnyWinningBet ? "Winning bet hit!" : "No winning bets.");
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
    }
}
