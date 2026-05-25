using System;
using System.Collections;
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

    private int activeReturnAnimations;
    private bool hasPendingBetChip;
    private ChipDenomination pendingBetChipDenomination;
    private Vector3 pendingBetChipTarget;
    private Action pendingBetChipArrived;

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

        if (activeReturnAnimations > 0 && GetDisplayedChipTotal() == currentChips)
            return;

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
        hasPendingBetChip = true;
        pendingBetChipDenomination = denomination;
        pendingBetChipTarget = worldTargetPosition;
        pendingBetChipArrived = onArrived;
    }

    public void ClearNextBetChipTarget()
    {
        hasPendingBetChip = false;
        pendingBetChipDenomination = default;
        pendingBetChipTarget = Vector3.zero;
        pendingBetChipArrived = null;
    }

    public void ReturnChipsToBalance(List<ChipStackView.ReturnChip> returnChips)
    {
        if (returnChips == null || returnChips.Count == 0)
            return;

        HashSet<ChipVisual> returningVisuals = new HashSet<ChipVisual>();

        for (int i = 0; i < returnChips.Count; i++)
        {
            ChipVisual chip = returnChips[i].Visual;

            if (chip == null)
                continue;

            chip.transform.SetParent(displayRoot, true);
            activeChips.Add(new DisplayedChip(returnChips[i].Denomination, chip));
            returningVisuals.Add(chip);
        }

        SortChips();

        for (int i = 0; i < activeChips.Count; i++)
        {
            DisplayedChip chip = activeChips[i];
            Vector3 targetPosition = stackSpacing * i;
            Quaternion targetRotation = Quaternion.Euler(stackRotation);
            Vector3 targetScale = Vector3.one * chipScale;

            if (returningVisuals.Contains(chip.Visual))
            {
                activeReturnAnimations++;
                StartCoroutine(AnimateReturnedChip(chip.Visual, targetPosition, targetRotation, targetScale));
                continue;
            }

            ApplyChipTransform(chip.Visual.transform, targetPosition, targetRotation, targetScale);
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

            SortAndLayoutChips();
        }

        return remaining == 0;
    }

    private bool RemoveDenominationAmount(ChipDenomination denomination)
    {
        int chipIndex = FindChipIndex(denomination);

        while (chipIndex < 0)
        {
            if (!TryBreakLargerChip((int)denomination))
                return false;

            SortAndLayoutChips();
            chipIndex = FindChipIndex(denomination);
        }

        ReleaseChipAt(chipIndex, ShouldFlyToBet(denomination));
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

            ReleaseChipAt(index, false);
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
            ReleaseChipAt(chipIndex, false);
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

    private void ReleaseChipAt(int index, bool flyToBet)
    {
        DisplayedChip chip = activeChips[index];
        activeChips.RemoveAt(index);

        if (flyToBet)
        {
            flyingChips.Add(chip.Visual);
            StartCoroutine(FlyChipToBet(chip.Visual, pendingBetChipTarget, pendingBetChipArrived));
            ClearNextBetChipTarget();
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

    private bool ShouldFlyToBet(ChipDenomination denomination)
    {
        return hasPendingBetChip && pendingBetChipDenomination == denomination;
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

    private IEnumerator AnimateReturnedChip(ChipVisual chip, Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale)
    {
        Transform chipTransform = chip.transform;
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
        activeReturnAnimations--;
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
        ClearNextBetChipTarget();
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
