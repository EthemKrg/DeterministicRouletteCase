using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChipStackView : MonoBehaviour
{
    public struct ReturnChip
    {
        public readonly ChipDenomination Denomination;
        public readonly ChipVisual Visual;

        public ReturnChip(ChipDenomination denomination, ChipVisual visual)
        {
            Denomination = denomination;
            Visual = visual;
        }
    }

    [SerializeField] private Transform chipRoot;
    [SerializeField] private TMP_Text totalStakeLabel;
    [SerializeField] private Vector3 chipStackOffset = new Vector3(0f, 0.025f, 0f);

    private readonly List<ReturnChip> chips = new List<ReturnChip>();

    private ChipVisualPool chipPool;
    private Vector3 defaultTotalStakeLabelLocalPos;

    public void Initialize(ChipVisualPool pool)
    {
        chipPool = pool;

        if (totalStakeLabel != null)
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

        if (totalStakeLabel != null)
            totalStakeLabel.transform.localPosition = defaultTotalStakeLabelLocalPos + chipStackOffset * chipIndex;

        chips.Add(new ReturnChip(denomination, chip));

        SetTotalStake(totalStake);
    }

    public Vector3 GetNextChipWorldPosition()
    {
        Transform root = chipRoot != null ? chipRoot : transform;
        return root.TransformPoint(chipStackOffset * chips.Count);
    }

    public List<ReturnChip> TakeChipsForReturn()
    {
        List<ReturnChip> returnChips = new List<ReturnChip>(chips);

        chips.Clear();

        if (totalStakeLabel != null)
            totalStakeLabel.text = string.Empty;

        return returnChips;
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
                chipPool.Release(chips[i].Visual);
        }

        chips.Clear();

        if (totalStakeLabel != null)
            totalStakeLabel.text = string.Empty;
    }
}
