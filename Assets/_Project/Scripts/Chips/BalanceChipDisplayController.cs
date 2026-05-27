using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BalanceChipDisplayController : MonoBehaviour
{
    private const int CompactTriggerChipCount = 10;
    private const int CompactBatchChipCount = 5;

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

    private class PendingBetChip
    {
        public readonly ChipDenomination Denomination;
        public readonly Vector3 Target;
        public readonly Action OnArrived;

        public PendingBetChip(ChipDenomination denomination, Vector3 target, Action onArrived)
        {
            Denomination = denomination;
            Target = target;
            OnArrived = onArrived;
        }
    }

    private struct ChipPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;

        public ChipPose(Transform source)
        {
            Position = source.position;
            Rotation = source.rotation;
            Scale = source.localScale;
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

    [Header("Animation")]
    [SerializeField] private float betChipMoveDuration = 0.42f;
    [SerializeField] private float betChipArcHeight = 0.25f;
    [SerializeField] private Vector3 betChipTargetRotation = Vector3.zero;
    [SerializeField] private AnimationCurve betChipMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly List<DisplayedChip> activeChips = new List<DisplayedChip>();
    private readonly List<ChipVisual> flyingChips = new List<ChipVisual>();
    private readonly List<DisplayedChip> returningChips = new List<DisplayedChip>();
    private readonly List<PendingBetChip> pendingBetChips = new List<PendingBetChip>();

    private int activeReturnAnimations;

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
            balanceText.text = $" Balance:\n {currentChips}";

        int targetActiveChips = Mathf.Max(0, currentChips - GetReturningChipTotal());

        if (activeReturnAnimations > 0 && GetDisplayedChipTotal() == targetActiveChips)
            return;

        if (activeChips.Count == 0)
        {
            RebuildChips(targetActiveChips);
            return;
        }

        if (!ApplyBalanceDifference(targetActiveChips))
        {
            RebuildChips(targetActiveChips);
            return;
        }

        CompactChipsIfNeeded();
        SortAndLayoutChips();
    }

    private bool ApplyBalanceDifference(int targetAmount)
    {
        int difference = targetAmount - GetDisplayedChipTotal();

        if (difference == 0)
            return true;

        bool updated = difference > 0 ? AddAmount(difference) : RemoveAmount(-difference);
        return updated && GetDisplayedChipTotal() == targetAmount;
    }

    public void SetNextBetChipTarget(ChipDenomination denomination, Vector3 worldTargetPosition, Action onArrived)
    {
        pendingBetChips.Add(new PendingBetChip(denomination, worldTargetPosition, onArrived));
    }

    public void ClearNextBetChipTarget()
    {
        if (pendingBetChips.Count == 0)
            return;

        pendingBetChips.RemoveAt(pendingBetChips.Count - 1);
    }

    public void ReturnChipsToBalance(List<ChipStackView.ReturnChip> returnChips)
    {
        if (returnChips == null || returnChips.Count == 0)
            return;

        for (int i = 0; i < returnChips.Count; i++)
        {
            ChipVisual chip = returnChips[i].Visual;

            if (chip == null)
                continue;

            chip.transform.SetParent(displayRoot, true);
            DisplayedChip returningChip = new DisplayedChip(returnChips[i].Denomination, chip);

            int landingIndex = GetReturnLandingIndex(returningChip.Denomination);
            returningChips.Add(returningChip);

            Vector3 targetPosition = stackSpacing * landingIndex;
            Quaternion targetRotation = Quaternion.Euler(stackRotation);
            Vector3 targetScale = Vector3.one * chipScale;

            activeReturnAnimations++;
            StartCoroutine(AnimateReturnedChip(returningChip, targetPosition, targetRotation, targetScale));
        }
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
        return AddAmount(chipAmount, 0, null);
    }

    private bool AddAmount(int chipAmount, int startIndex, ChipPose? startPose)
    {
        int remaining = chipAmount;

        for (int i = startIndex; i < DisplayOrder.Length; i++)
        {
            ChipDenomination denomination = DisplayOrder[i];
            int chipValue = (int)denomination;
            int count = remaining / chipValue;

            for (int j = 0; j < count; j++)
                AddChip(denomination, startPose);

            remaining %= chipValue;
        }

        return remaining == 0;
    }

    private bool RemoveAmount(int chipAmount)
    {
        if (pendingBetChips.Count > 0)
        {
            if (!RemovePendingBetChips(chipAmount, out int remaining))
                return false;

            return remaining == 0 || RemoveRegularAmount(remaining);
        }

        return RemoveRegularAmount(chipAmount);
    }

    private bool RemovePendingBetChips(int chipAmount, out int remaining)
    {
        remaining = chipAmount;

        while (remaining > 0 && pendingBetChips.Count > 0)
        {
            PendingBetChip pendingBetChip = pendingBetChips[0];

            if ((int)pendingBetChip.Denomination > remaining)
                break;

            if (!RemovePendingBetChip(pendingBetChip))
                return false;

            remaining -= (int)pendingBetChip.Denomination;
        }

        return true;
    }

    private bool RemoveRegularAmount(int chipAmount)
    {
        if (TryGetDenomination(chipAmount, out ChipDenomination denomination))
            return RemoveChipByDenomination(denomination);

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

            SortAndLayoutChips();
        }

        return remaining == 0;
    }

    private bool RemoveChipByDenomination(ChipDenomination denomination)
    {
        int chipIndex = FindChipIndex(denomination);

        while (chipIndex < 0)
        {
            if (!TryBreakLargerChip((int)denomination))
                return false;

            SortAndLayoutChips();
            chipIndex = FindChipIndex(denomination);
        }

        ReleaseChipAt(chipIndex, null);
        return true;
    }

    private bool RemovePendingBetChip(PendingBetChip pendingBetChip)
    {
        int chipIndex = FindChipIndex(pendingBetChip.Denomination);

        while (chipIndex < 0)
        {
            if (!TryBreakLargerChip((int)pendingBetChip.Denomination))
                return false;

            SortAndLayoutChips();
            chipIndex = FindChipIndex(pendingBetChip.Denomination);
        }

        pendingBetChips.Remove(pendingBetChip);
        ReleaseChipAt(chipIndex, pendingBetChip);
        return true;
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

            ReleaseChipAt(index, null);
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

            ChipPose brokenChipPose = new ChipPose(activeChips[chipIndex].Visual.transform);
            ReleaseChipAt(chipIndex, null);
            return AddAmount((int)denomination, i + 1, brokenChipPose);
        }

        return false;
    }

    private void AddChip(ChipDenomination denomination)
    {
        AddChip(denomination, null);
    }

    private void AddChip(ChipDenomination denomination, ChipPose? startPose)
    {
        ChipVisual chip = chipVisualPool.Get(displayRoot);
        chip.Setup(denomination);

        if (startPose.HasValue)
        {
            ChipPose pose = startPose.Value;
            chip.transform.position = pose.Position;
            chip.transform.rotation = pose.Rotation;
            chip.transform.localScale = pose.Scale;
        }

        activeChips.Add(new DisplayedChip(denomination, chip));
    }

    private void ReleaseChipAt(int index, PendingBetChip pendingBetChip)
    {
        DisplayedChip chip = activeChips[index];
        activeChips.RemoveAt(index);

        if (pendingBetChip != null)
        {
            flyingChips.Add(chip.Visual);
            StartCoroutine(FlyChipToBet(chip.Visual, pendingBetChip.Target, pendingBetChip.OnArrived));
            return;
        }

        chipVisualPool.Release(chip.Visual);
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

    private int GetReturningChipTotal()
    {
        int total = 0;

        foreach (DisplayedChip chip in returningChips)
            total += (int)chip.Denomination;

        return total;
    }

    private int GetReturnLandingIndex(ChipDenomination denomination)
    {
        int index = 0;
        int chipValue = (int)denomination;

        for (int i = 0; i < activeChips.Count; i++)
        {
            if ((int)activeChips[i].Denomination > chipValue)
                index++;
        }

        for (int i = 0; i < returningChips.Count; i++)
        {
            int returningValue = (int)returningChips[i].Denomination;

            if (returningValue >= chipValue)
                index++;
        }

        return index;
    }

    private void SortChips()
    {
        activeChips.Sort((first, second) => ((int)second.Denomination).CompareTo((int)first.Denomination));
    }

    private void SortAndLayoutChips()
    {
        SortChips();
        LayoutChips();
    }

    private void LayoutChips()
    {
        for (int i = 0; i < activeChips.Count; i++)
        {
            Transform chipTransform = activeChips[i].Visual.transform;
            ApplyChipTransform(
                chipTransform,
                stackSpacing * i,
                Quaternion.Euler(stackRotation),
                Vector3.one * chipScale);
        }
    }

    private IEnumerator FlyChipToBet(ChipVisual chip, Vector3 targetPosition, Action onArrived)
    {
        Transform chipTransform = chip.transform;
        Vector3 startPosition = chipTransform.position;
        Quaternion startRotation = chipTransform.rotation;
        Quaternion targetRotation = Quaternion.Euler(betChipTargetRotation);
        float elapsed = 0f;

        while (elapsed < betChipMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = betChipMoveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / betChipMoveDuration);
            float easedT = betChipMoveCurve.Evaluate(t);

            chipTransform.position = GetArcPosition(startPosition, targetPosition, easedT);
            chipTransform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, easedT);

            yield return null;
        }

        chipTransform.position = targetPosition;
        chipTransform.rotation = targetRotation;

        flyingChips.Remove(chip);
        chipVisualPool.Release(chip);
        onArrived?.Invoke();
    }

    private Vector3 GetArcPosition(Vector3 startPosition, Vector3 targetPosition, float t)
    {
        Vector3 position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * betChipArcHeight;
        return position;
    }

    private IEnumerator AnimateReturnedChip(DisplayedChip chip, Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale)
    {
        Transform chipTransform = chip.Visual.transform;
        Vector3 startPosition = chipTransform.localPosition;
        Quaternion startRotation = chipTransform.localRotation;
        Vector3 startScale = chipTransform.localScale;
        float elapsed = 0f;

        while (elapsed < betChipMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = betChipMoveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / betChipMoveDuration);
            float easedT = betChipMoveCurve.Evaluate(t);

            chipTransform.localPosition = GetArcPosition(startPosition, targetPosition, easedT);
            chipTransform.localRotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, easedT);
            chipTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedT);

            yield return null;
        }

        ApplyChipTransform(chipTransform, targetPosition, targetRotation, targetScale);
        returningChips.Remove(chip);
        activeChips.Add(chip);
        activeReturnAnimations--;

        if (activeReturnAnimations == 0)
            CompactChipsIfNeeded();

        SortAndLayoutChips();
    }

    private void CompactChipsIfNeeded()
    {
        while (TryGetCompactDenomination(out ChipDenomination denomination))
            CompactChipBatch(denomination);
    }

    private bool TryGetCompactDenomination(out ChipDenomination denomination)
    {
        for (int i = 1; i < DisplayOrder.Length; i++)
        {
            int count = 0;
            denomination = DisplayOrder[i];

            for (int j = 0; j < activeChips.Count; j++)
            {
                if (activeChips[j].Denomination != denomination)
                    continue;

                count++;

                if (count >= CompactTriggerChipCount)
                    return true;
            }
        }

        denomination = default;
        return false;
    }

    private void CompactChipBatch(ChipDenomination denomination)
    {
        int removedCount = 0;

        for (int i = activeChips.Count - 1; i >= 0 && removedCount < CompactBatchChipCount; i--)
        {
            if (activeChips[i].Denomination != denomination)
                continue;

            chipVisualPool.Release(activeChips[i].Visual);
            activeChips.RemoveAt(i);
            removedCount++;
        }

        AddAmount((int)denomination * removedCount);
    }

    private void ApplyChipTransform(Transform chipTransform, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        chipTransform.localPosition = localPosition;
        chipTransform.localRotation = localRotation;
        chipTransform.localScale = localScale;
    }

    private void ClearVisuals()
    {
        if (chipVisualPool == null)
            return;

        StopAllCoroutines();
        activeReturnAnimations = 0;

        for (int i = activeChips.Count - 1; i >= 0; i--)
            chipVisualPool.Release(activeChips[i].Visual);

        activeChips.Clear();

        for (int i = flyingChips.Count - 1; i >= 0; i--)
            chipVisualPool.Release(flyingChips[i]);

        flyingChips.Clear();

        for (int i = returningChips.Count - 1; i >= 0; i--)
            chipVisualPool.Release(returningChips[i].Visual);

        returningChips.Clear();
        pendingBetChips.Clear();
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
