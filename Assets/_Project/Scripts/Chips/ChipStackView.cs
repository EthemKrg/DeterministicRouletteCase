using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChipStackView : MonoBehaviour
{
    [SerializeField] private Transform chipRoot;
    [SerializeField] private TMP_Text totalStakeLabel;
    [SerializeField] private Transform totalStakeLabelRoot;
    [SerializeField] private Vector3 chipStackOffset = new Vector3(0f, 0.025f, 0f);
    [SerializeField] private Vector3 labelBaseOffset = new Vector3(0f, 0.08f, 0f);

    private readonly List<ChipVisual> chips = new List<ChipVisual>();

    private ChipVisualPool chipPool;

    public void Initialize(ChipVisualPool pool)
    {
        chipPool = pool;
    }

    public void AddChip(ChipDenomination denomination, int totalStake)
    {
        if (chipPool == null)
            throw new System.InvalidOperationException($"{nameof(ChipStackView)} is missing ChipVisualPool.");

        Transform root = chipRoot != null ? chipRoot : transform;

        ChipVisual chip = chipPool.Get(root);
        chip.Setup(denomination);

        int chipIndex = chips.Count;
        chip.transform.localPosition = chipStackOffset * chipIndex;
        chip.transform.localRotation = Quaternion.identity;
        chip.transform.localScale = Vector3.one;

        chips.Add(chip);

        SetTotalStake(totalStake);
        UpdateLabelPosition();
    }

    public void SetTotalStake(int totalStake)
    {
        if (totalStakeLabel != null)
            totalStakeLabel.text = totalStake.ToString();
    }

    public void Clear()
    {
        if (chipPool != null)
        {
            for (int i = chips.Count - 1; i >= 0; i--)
                chipPool.Release(chips[i]);
        }

        chips.Clear();

        if (totalStakeLabel != null)
            totalStakeLabel.text = string.Empty;
    }

    private void UpdateLabelPosition()
    {
        if (totalStakeLabelRoot == null)
            return;

        totalStakeLabelRoot.localPosition = labelBaseOffset + chipStackOffset * chips.Count;
    }
}