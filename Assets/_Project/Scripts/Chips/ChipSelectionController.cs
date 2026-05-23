using System;
using UnityEngine;

public class ChipSelectionController : MonoBehaviour
{
    [SerializeField] private ChipDenomination selectedChip = ChipDenomination.Chip10;

    public int SelectedChipValue => (int)selectedChip;

    public event Action<int> OnSelectedChipChanged;

    public void SelectChip10()
    {
        SetSelectedChip(ChipDenomination.Chip10);
    }

    public void SelectChip25()
    {
        SetSelectedChip(ChipDenomination.Chip25);
    }

    public void SelectChip50()
    {
        SetSelectedChip(ChipDenomination.Chip50);
    }

    public void SelectChip100()
    {
        SetSelectedChip(ChipDenomination.Chip100);
    }

    public void SelectChip500()
    {
        SetSelectedChip(ChipDenomination.Chip500);
    }

    public void SetSelectedChip(ChipDenomination chip)
    {
        if (selectedChip == chip)
            return;

        selectedChip = chip;
        OnSelectedChipChanged?.Invoke(SelectedChipValue);
    }
}