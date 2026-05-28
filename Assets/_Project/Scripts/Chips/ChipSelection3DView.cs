using UnityEngine;

public class ChipSelection3DView : MonoBehaviour
{
    [SerializeField] private ChipSelectionController chipSelectionController;
    [SerializeField] private RouletteSoundManager soundManager;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipSelection3DButton[] buttons;
    
    private GameFlowState lastFlowState;

    private void Awake()
    {
        if (chipSelectionController == null)
            throw new System.InvalidOperationException($"{nameof(ChipSelection3DView)} needs a ChipSelectionController reference.");

        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(ChipSelection3DView)} needs a GameFlowController reference.");
    }

    private void OnEnable()
    {
        chipSelectionController.OnSelectedChipChanged += HandleSelectedChipChanged;
        gameFlowController.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        chipSelectionController.OnSelectedChipChanged -= HandleSelectedChipChanged;
        gameFlowController.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        lastFlowState = gameFlowController.GameState.FlowState;

        chipSelectionController.SelectDefaultChip();
        RefreshSelection(chipSelectionController.SelectedChipValue);
    }

    public void Select(ChipDenomination denomination)
    {
        if (!gameFlowController.TryCanAcceptGameplayInput(GameplayInputKind.ChipSelection, out _))
            return;

        if (soundManager != null)
            soundManager.PlayButtonClick();

        chipSelectionController.SetSelectedChip(denomination);
        RefreshSelection((int)denomination);
    }

    private void HandleSelectedChipChanged(int selectedValue)
    {
        RefreshSelection(selectedValue);
    }

    private void HandleGameStateChanged()
    {
        GameFlowState currentFlowState = gameFlowController.GameState.FlowState;

        if (currentFlowState == lastFlowState)
            return;

        lastFlowState = currentFlowState;

        if (currentFlowState == GameFlowState.Betting)
        {
            chipSelectionController.SelectDefaultChip();
            RefreshSelection(chipSelectionController.SelectedChipValue);
            return;
        }

        ResetAll();
    }

    private void RefreshSelection(int selectedValue)
    {
        foreach (ChipSelection3DButton button in buttons)
        {
            if (button == null)
                continue;

            button.SetSelected((int)button.Denomination == selectedValue);
        }
    }

    private void ResetAll()
    {
        foreach (ChipSelection3DButton button in buttons)
        {
            if (button == null)
                continue;

            button.ResetVisual();
        }
    }
}
