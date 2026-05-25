using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BalanceChipDisplayController : MonoBehaviour
{
    private class DisplayedChip
    {
        public readonly ChipDenomination Denomination;
        public readonly ChipVisual Visual;

        public DisplayedChip(ChipDenomination denomination, ChipVisual visual)
        {
            Denomination = denomination;
            Visual = visual;
        }
    }

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
    [SerializeField] private Vector3 stackSpacing = new Vector3(0f, 0f, 0.38f);
    [SerializeField] private Vector3 stackRotation = new Vector3(-60f, 0f, 0f);
    [SerializeField] private float chipScale = 0.75f;

    private readonly List<DisplayedChip> activeChips = new List<DisplayedChip>();

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

        int currentChips = gameFlowController.GameState.CurrentChips;

        if (balanceText != null)
            balanceText.text = $"Current Balance:\n {currentChips}";

        if (activeChips.Count == 0)
        {
            RebuildChips(currentChips);
            return;
        }

        if (!ApplyBalanceDifference(currentChips))
        {
            RebuildChips(currentChips);
            return;
        }

        SortChips();
        LayoutChips();
    }

    private bool ApplyBalanceDifference(int targetAmount)
    {
        int difference = targetAmount - GetDisplayedChipTotal();

        if (difference == 0)
            return true;

        bool updated = difference > 0 ? AddAmount(difference) : RemoveAmount(-difference);
        return updated && GetDisplayedChipTotal() == targetAmount;
    }

    private void RebuildChips(int chipAmount)
    {
        ClearVisuals();

        List<ChipDenomination> displayChips = BuildInitialChips(chipAmount);

        for (int i = 0; i < displayChips.Count; i++)
            AddChip(displayChips[i]);

        LayoutChips();
    }

    private List<ChipDenomination> BuildInitialChips(int chipAmount)
    {
        List<ChipDenomination> chips = new List<ChipDenomination>();
        int remaining = chipAmount;

        AddInitialChips(chips, ChipDenomination.Chip5000, 6, ref remaining);
        AddInitialChips(chips, ChipDenomination.Chip2000, 5, ref remaining);
        AddInitialChips(chips, ChipDenomination.Chip1000, 6, ref remaining);
        AddInitialChips(chips, ChipDenomination.Chip500, 6, ref remaining);
        AddInitialChips(chips, ChipDenomination.Chip250, 4, ref remaining);

        AddBreakdownChips(chips, remaining, 0);

        chips.Sort((first, second) => ((int)second).CompareTo((int)first));
        return chips;
    }

    private void AddInitialChips(List<ChipDenomination> chips, ChipDenomination denomination, int preferredCount, ref int remaining)
    {
        int chipValue = (int)denomination;
        int count = Mathf.Min(preferredCount, remaining / chipValue);

        for (int i = 0; i < count; i++)
        {
            chips.Add(denomination);
            remaining -= chipValue;
        }
    }

    private void AddBreakdownChips(List<ChipDenomination> chips, int chipAmount, int startIndex)
    {
        int remaining = chipAmount;

        for (int i = startIndex; i < DisplayOrder.Length; i++)
        {
            ChipDenomination denomination = DisplayOrder[i];
            int chipValue = (int)denomination;
            int count = remaining / chipValue;

            for (int j = 0; j < count; j++)
                chips.Add(denomination);

            remaining %= chipValue;
        }
    }

    private bool AddAmount(int chipAmount)
    {
        return AddAmount(chipAmount, 0);
    }

    private bool AddAmount(int chipAmount, int startIndex)
    {
        int remaining = chipAmount;

        for (int i = startIndex; i < DisplayOrder.Length; i++)
        {
            ChipDenomination denomination = DisplayOrder[i];
            int chipValue = (int)denomination;
            int count = remaining / chipValue;

            for (int j = 0; j < count; j++)
                AddChip(denomination);

            remaining %= chipValue;
        }

        return remaining == 0;
    }

    private bool RemoveAmount(int chipAmount)
    {
        if (TryGetDenomination(chipAmount, out ChipDenomination denomination))
            return RemoveDenominationAmount(denomination);

        int remaining = chipAmount;

        while (remaining > 0)
        {
            if (TryRemoveChipAtOrBelow(remaining, out ChipDenomination removedDenomination))
            {
                remaining -= (int)removedDenomination;
                continue;
            }

            if (!TryBreakLargerChip(remaining))
                return false;
        }

        return remaining == 0;
    }

    private bool RemoveDenominationAmount(ChipDenomination denomination)
    {
        while (true)
        {
            int chipIndex = FindChipIndex(denomination);
            if (chipIndex >= 0)
            {
                ReleaseChipAt(chipIndex);
                return true;
            }

            if (!TryBreakLargerChip((int)denomination))
                return false;
        }
    }

    private bool TryGetDenomination(int chipAmount, out ChipDenomination denomination)
    {
        foreach (ChipDenomination displayDenomination in DisplayOrder)
        {
            if ((int)displayDenomination != chipAmount)
                continue;

            denomination = displayDenomination;
            return true;
        }

        denomination = default;
        return false;
    }

    private bool TryRemoveChipAtOrBelow(int chipAmount, out ChipDenomination removedDenomination)
    {
        foreach (ChipDenomination denomination in DisplayOrder)
        {
            if ((int)denomination > chipAmount)
                continue;

            int index = FindChipIndex(denomination);
            if (index < 0)
                continue;

            ReleaseChipAt(index);
            removedDenomination = denomination;
            return true;
        }

        removedDenomination = default;
        return false;
    }

    private bool TryBreakLargerChip(int chipAmount)
    {
        for (int i = DisplayOrder.Length - 1; i >= 0; i--)
        {
            ChipDenomination denomination = DisplayOrder[i];

            if ((int)denomination <= chipAmount)
                continue;

            int chipIndex = FindChipIndex(denomination);
            if (chipIndex < 0)
                continue;

            ReleaseChipAt(chipIndex);
            return AddAmount((int)denomination, i + 1);
        }

        return false;
    }

    private void AddChip(ChipDenomination denomination)
    {
        ChipVisual chip = chipVisualPool.Get(displayRoot);
        chip.Setup(denomination);

        activeChips.Add(new DisplayedChip(denomination, chip));
    }

    private void ReleaseChipAt(int index)
    {
        chipVisualPool.Release(activeChips[index].Visual);
        activeChips.RemoveAt(index);
    }

    private int FindChipIndex(ChipDenomination denomination)
    {
        for (int i = 0; i < activeChips.Count; i++)
        {
            if (activeChips[i].Denomination == denomination)
                return i;
        }

        return -1;
    }

    private int GetDisplayedChipTotal()
    {
        int total = 0;

        foreach (DisplayedChip chip in activeChips)
            total += (int)chip.Denomination;

        return total;
    }

    private void SortChips()
    {
        activeChips.Sort((first, second) => ((int)second.Denomination).CompareTo((int)first.Denomination));
    }

    private void LayoutChips()
    {
        for (int i = 0; i < activeChips.Count; i++)
        {
            Transform chipTransform = activeChips[i].Visual.transform;
            chipTransform.localPosition = stackSpacing * i;
            chipTransform.localEulerAngles = stackRotation;
            chipTransform.localScale = Vector3.one * chipScale;
        }
    }

    private void ClearVisuals()
    {
        if (chipVisualPool == null)
            return;

        for (int i = activeChips.Count - 1; i >= 0; i--)
            chipVisualPool.Release(activeChips[i].Visual);

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
