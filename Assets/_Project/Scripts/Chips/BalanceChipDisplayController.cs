using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BalanceChipDisplayController : MonoBehaviour
{
    private static readonly ChipDenomination[] DisplayOrder =
    {
        ChipDenomination.Chip5000,
        ChipDenomination.Chip2000,
        ChipDenomination.Chip1000,
        ChipDenomination.Chip500,
        ChipDenomination.Chip250
    };

    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipVisualPool chipVisualPool;
    [SerializeField] private Transform displayRoot;
    [SerializeField] private TMP_Text balanceText;

    [Header("Layout")]
    [SerializeField] private Vector3 stackSpacing = new Vector3(0.38f, 0f, 0f);
    [SerializeField] private Vector3 chipVerticalOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private float chipScale = 0.75f;

    private readonly List<ChipVisual> activeChips = new List<ChipVisual>();

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
            gameFlowController.OnGameStateChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
            gameFlowController.OnGameStateChanged -= Refresh;

        ClearVisuals();
    }

    private void Refresh()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
            return;

        ClearVisuals();

        int currentChips = gameFlowController.GameState.CurrentChips;

        if (balanceText != null)
            balanceText.text = $"Current Balance:\n {currentChips}";

        int remaining = currentChips;
        int stackIndex = 0;

        foreach (ChipDenomination denomination in DisplayOrder)
        {
            int chipValue = (int)denomination;
            int count = remaining / chipValue;
            remaining %= chipValue;

            if (count <= 0)
                continue;

            DrawStack(denomination, count, stackIndex);
            stackIndex++;
        }
    }

    private void DrawStack(ChipDenomination denomination, int count, int stackIndex)
    {
        Vector3 stackBasePosition = stackSpacing * stackIndex;

        for (int i = 0; i < count; i++)
        {
            ChipVisual chip = chipVisualPool.Get(displayRoot);
            chip.Setup(denomination);

            Transform chipTransform = chip.transform;
            chipTransform.localPosition = stackBasePosition + chipVerticalOffset * i;
            chipTransform.localEulerAngles = new Vector3(60f, 0f, 0f);
            chipTransform.localScale = Vector3.one * chipScale;

            activeChips.Add(chip);
        }
    }

    private void ClearVisuals()
    {
        if (chipVisualPool == null)
            return;

        for (int i = activeChips.Count - 1; i >= 0; i--)
            chipVisualPool.Release(activeChips[i]);

        activeChips.Clear();
    }

    private void ValidateReferences()
    {
        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(BalanceChipDisplayController)} needs a GameFlowController reference.");

        if (chipVisualPool == null)
            throw new System.InvalidOperationException($"{nameof(BalanceChipDisplayController)} needs a ChipVisualPool reference.");

        if (displayRoot == null)
            throw new System.InvalidOperationException($"{nameof(BalanceChipDisplayController)} needs a display root reference.");

        if (balanceText == null)
            throw new System.InvalidOperationException($"{nameof(BalanceChipDisplayController)} needs a balance text reference.");
    }
}
