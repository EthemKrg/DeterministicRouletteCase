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
    [SerializeField] private Vector3 hitBoxSize = new Vector3(0.35f, 0.18f, 0.35f);

    private readonly List<ReturnChip> chips = new List<ReturnChip>();

    private ChipVisualPool chipPool;
    private Vector3 defaultTotalStakeLabelLocalPos;
    private BoxCollider hitCollider;

    public int ChipCount => chips.Count;
    public Vector3 ChipStackOffset => chipStackOffset;

    public void Initialize(ChipVisualPool pool)
    {
        chipPool = pool;

        if (totalStakeLabel != null)
            defaultTotalStakeLabelLocalPos = totalStakeLabel.transform.localPosition;

        EnsureHitCollider();
        ClearStakeLabel();
        RefreshHitCollider();
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
        RefreshHitCollider();

        SetTotalStake(totalStake);
    }

    public Vector3 GetChipWorldPosition(int chipIndex)
    {
        Transform root = chipRoot != null ? chipRoot : transform;
        return root.TransformPoint(chipStackOffset * chipIndex);
    }

    public bool TryTakeTopChip(out ReturnChip chip)
    {
        chip = default;

        if (!TryPeekTopChip(out chip))
            return false;

        int chipIndex = chips.Count - 1;
        chips.RemoveAt(chipIndex);
        chip.Visual.transform.SetParent(null, true);
        RefreshHitCollider();

        return true;
    }

    public bool TryPeekTopChip(out ReturnChip chip)
    {
        chip = default;

        if (chips.Count == 0)
            return false;

        chip = chips[chips.Count - 1];
        return true;
    }

    public List<ReturnChip> TakeChipsForReturn()
    {
        List<ReturnChip> returnChips = new List<ReturnChip>(chips);

        for (int i = 0; i < returnChips.Count; i++)
            returnChips[i].Visual.transform.SetParent(null, true);

        chips.Clear();
        RefreshHitCollider();
        ClearStakeLabel();

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

        ResetView();
    }

    public void ResetView()
    {
        chips.Clear();
        RefreshHitCollider();
        ClearStakeLabel();
    }

    public void RefreshStakeLabel(int totalStake)
    {
        SetTotalStake(totalStake);

        if (totalStakeLabel == null)
            return;

        int topChipIndex = Mathf.Max(0, chips.Count - 1);
        totalStakeLabel.transform.localPosition = defaultTotalStakeLabelLocalPos + chipStackOffset * topChipIndex;
    }

    private void EnsureHitCollider()
    {
        if (hitCollider != null)
            return;

        hitCollider = GetComponent<BoxCollider>();

        if (hitCollider == null)
            hitCollider = gameObject.AddComponent<BoxCollider>();

        hitCollider.isTrigger = true;
    }

    private void RefreshHitCollider()
    {
        if (hitCollider == null)
            return;

        int visibleChipCount = Mathf.Max(1, chips.Count);
        Vector3 topOffset = chipStackOffset * (visibleChipCount - 1);
        hitCollider.center = topOffset * 0.5f;
        hitCollider.size = hitBoxSize + Abs(topOffset);
        hitCollider.enabled = chips.Count > 0;
    }

    private Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void ClearStakeLabel()
    {
        if (totalStakeLabel == null)
            return;

        totalStakeLabel.text = string.Empty;
        totalStakeLabel.transform.localPosition = defaultTotalStakeLabelLocalPos;
    }
}
