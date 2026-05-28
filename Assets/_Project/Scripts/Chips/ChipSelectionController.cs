using System;
using UnityEngine;

public class ChipSelectionController : MonoBehaviour
{
    [SerializeField] private ChipDenomination selectedChip = ChipDenomination.Chip250;
    [SerializeField] private ChipDenomination defaultChip = ChipDenomination.Chip250;

    public ChipDenomination SelectedChip => selectedChip;
    public int SelectedChipValue => (int)selectedChip;

    public event Action<int> OnSelectedChipChanged;

    public void SelectChip250()
    {
        SetSelectedChip(ChipDenomination.Chip250);
    }

    public void SelectChip500()
    {
        SetSelectedChip(ChipDenomination.Chip500);
    }

    public void SelectChip1000()
    {
        SetSelectedChip(ChipDenomination.Chip1000);
    }

    public void SelectChip2000()
    {
        SetSelectedChip(ChipDenomination.Chip2000);
    }

    public void SelectChip5000()
    {
        SetSelectedChip(ChipDenomination.Chip5000);
    }

    public void SetSelectedChip(ChipDenomination chip)
    {
        if (selectedChip == chip)
            return;

        selectedChip = chip;
        OnSelectedChipChanged?.Invoke(SelectedChipValue);
    }

    public void SelectDefaultChip()
    {
        selectedChip = defaultChip;
        OnSelectedChipChanged?.Invoke(SelectedChipValue);
    }

    public int ExportSelectedChipValue()
    {
        return SelectedChipValue;
    }

    public void RestoreSelectedChip(int chipValue)
    {
        if (Enum.IsDefined(typeof(ChipDenomination), chipValue))
        {
            SetSelectedChip((ChipDenomination)chipValue);
        }
        else
        {
            Debug.LogWarning($"Saved chip value {chipValue} is not a valid denomination. Falling back to default.");
            SelectDefaultChip();
        }
    }
}