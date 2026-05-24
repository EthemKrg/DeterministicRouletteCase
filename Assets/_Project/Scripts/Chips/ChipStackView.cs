using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChipStackView : MonoBehaviour
{
    [SerializeField] private Transform chipRoot;
    [SerializeField] private TMP_Text totalStakeLabel;
    [SerializeField] private Vector3 chipStackOffset = new Vector3(0f, 0.025f, 0f);

    private readonly List<ChipVisual> chips = new List<ChipVisual>();

    private ChipVisualPool chipPool;
    private Vector3 defaultTotalStakeLabelLocalPos;

    public void Initialize(ChipVisualPool pool)
    {
        chipPool = pool;
        defaultTotalStakeLabelLocalPos = totalStakeLabel.transform.localPosition;
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

        var stakeLabelPos = defaultTotalStakeLabelLocalPos + chipStackOffset * chipIndex;
        totalStakeLabel.transform.localPosition = stakeLabelPos;

        chips.Add(chip);

        SetTotalStake(totalStake);
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
}