using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChipStackViewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipStackView chipStackPrefab;
    [SerializeField] private ChipVisualPool chipVisualPool;
    [SerializeField] private BalanceChipDisplayController balanceChipDisplayController;
    [SerializeField] private RouletteSoundManager soundManager;
    [SerializeField] private Transform stackRoot;

    [Header("Placement")]
    [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.08f, 0f);

    private readonly Dictionary<RouletteBetArea, ChipStackView> stacksByArea = new Dictionary<RouletteBetArea, ChipStackView>();
    private readonly Dictionary<RouletteBetArea, int> stakesByArea = new Dictionary<RouletteBetArea, int>();
    private readonly Dictionary<RouletteBetArea, int> pendingChipsByArea = new Dictionary<RouletteBetArea, int>();
    private readonly List<ChipStackView> inactiveStacks = new List<ChipStackView>();

    private int stackVisualVersion;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnRoundResolved += HandleRoundResolved;
        gameFlowController.OnBetsCleared += HandleBetsCleared;
    }

    private void OnDisable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnRoundResolved -= HandleRoundResolved;
        gameFlowController.OnBetsCleared -= HandleBetsCleared;
    }

    public void ShowOrUpdateStack(RouletteBetArea betArea, ChipDenomination denomination, int addedStake)
    {
        ShowOrUpdateStack(betArea, denomination, addedStake, stackVisualVersion);
    }

    public void ShowOrUpdateStack(RouletteBetArea betArea, ChipDenomination denomination, int addedStake, int expectedVersion)
    {
        if (expectedVersion != stackVisualVersion)
            return;

        if (betArea == null || addedStake <= 0)
            return;

        if (!stakesByArea.ContainsKey(betArea))
            stakesByArea[betArea] = 0;

        stakesByArea[betArea] += addedStake;
        ReleasePendingChipReservation(betArea);

        ChipStackView stackView = GetOrCreateStack(betArea);
        stackView.AddChip(denomination, stakesByArea[betArea]);
    }

    public Vector3 GetStackPosition(RouletteBetArea betArea)
    {
        return betArea.transform.position + stackOffset;
    }

    public Vector3 ReserveNextChipWorldPosition(RouletteBetArea betArea)
    {
        if (!pendingChipsByArea.ContainsKey(betArea))
            pendingChipsByArea[betArea] = 0;

        int chipIndex = pendingChipsByArea[betArea];

        if (stacksByArea.TryGetValue(betArea, out ChipStackView stackView) && stackView != null)
        {
            chipIndex += stackView.ChipCount;
            pendingChipsByArea[betArea]++;
            return stackView.GetChipWorldPosition(chipIndex);
        }

        pendingChipsByArea[betArea]++;

        return GetStackPosition(betArea) + chipStackPrefab.ChipStackOffset * chipIndex;
    }

    public void CancelPendingChipReservation(RouletteBetArea betArea)
    {
        ReleasePendingChipReservation(betArea);
    }

    public int GetVisualVersion()
    {
        return stackVisualVersion;
    }

    public bool TryUndoTopChip(ChipStackView stackView)
    {
        if (!gameFlowController.TryCanAcceptGameplayInput(GameplayInputKind.BetUndo, out _))
            return false;

        RouletteBetArea betArea = GetBetAreaForStack(stackView);

        if (betArea == null || !stakesByArea.TryGetValue(betArea, out int currentStake))
            return false;

        if (!stackView.TryPeekTopChip(out ChipStackView.ReturnChip chip))
            return false;

        RouletteBet betTemplate = betArea.CreateBet((int)chip.Denomination, gameFlowController.GameState.WheelType);

        if (!gameFlowController.TryRemoveLastBet(betTemplate, out int refundedStake, false))
            return false;

        if (!stackView.TryTakeTopChip(out chip))
            return false;

        int updatedStake = currentStake - refundedStake;

        if (updatedStake > 0)
        {
            stakesByArea[betArea] = updatedStake;
            stackView.RefreshStakeLabel(updatedStake);
        }
        else
        {
            stacksByArea.Remove(betArea);
            stakesByArea.Remove(betArea);
            pendingChipsByArea.Remove(betArea);
            DeactivateStack(stackView);
        }

        if (soundManager != null)
            soundManager.PlayChipSound();

        if (balanceChipDisplayController != null)
            balanceChipDisplayController.ReturnChipsToBalance(new List<ChipStackView.ReturnChip> { chip });
        else
            chipVisualPool.Release(chip.Visual);

        gameFlowController.RefreshStateViews();
        return true;
    }

    public void ClearAll()
    {
        stackVisualVersion++;

        foreach (ChipStackView stackView in stacksByArea.Values)
        {
            if (stackView == null)
                continue;

            stackView.Clear();
            DeactivateStack(stackView);
        }

        stacksByArea.Clear();
        stakesByArea.Clear();
        pendingChipsByArea.Clear();
    }

    private ChipStackView GetOrCreateStack(RouletteBetArea betArea)
    {
        if (stacksByArea.TryGetValue(betArea, out ChipStackView existingStack) && existingStack != null)
            return existingStack;

        Transform root = stackRoot != null ? stackRoot : transform;

        ChipStackView stackView = GetInactiveStack();

        if (stackView == null)
            stackView = Instantiate(chipStackPrefab, root);

        stackView.Initialize(chipVisualPool);
        stackView.transform.position = GetStackPosition(betArea);
        stackView.transform.rotation = Quaternion.identity;
        stackView.transform.SetParent(root, true);
        stackView.gameObject.layer = betArea.gameObject.layer;
        stackView.gameObject.SetActive(true);

        stacksByArea[betArea] = stackView;

        return stackView;
    }

    private void HandleRoundResolved(RoundResult result)
    {
        ClearAll();
    }

    private void HandleBetsCleared()
    {
        if (balanceChipDisplayController != null)
        {
            stackVisualVersion++;
            List<ChipStackView.ReturnChip> returnChips = new List<ChipStackView.ReturnChip>();

            foreach (ChipStackView stackView in stacksByArea.Values)
            {
                if (stackView == null)
                    continue;

                returnChips.AddRange(stackView.TakeChipsForReturn());
                DeactivateStack(stackView);
            }

            stacksByArea.Clear();
            stakesByArea.Clear();
            pendingChipsByArea.Clear();

            if (soundManager != null)
                soundManager.PlayButtonClick();

            balanceChipDisplayController.ReturnChipsToBalance(returnChips);
            return;
        }

        ClearAll();
    }

    private void ValidateReferences()
    {
        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(ChipStackViewController)} needs a GameFlowController reference.");

        if (chipStackPrefab == null)
            throw new System.InvalidOperationException($"{nameof(ChipStackViewController)} needs a ChipStackView prefab reference.");

        if (chipVisualPool == null)
            throw new System.InvalidOperationException($"{nameof(ChipStackViewController)} needs a ChipVisualPool reference.");
    }

    private RouletteBetArea GetBetAreaForStack(ChipStackView stackView)
    {
        foreach (KeyValuePair<RouletteBetArea, ChipStackView> pair in stacksByArea)
        {
            if (pair.Value == stackView)
                return pair.Key;
        }

        return null;
    }

    private ChipStackView GetInactiveStack()
    {
        if (inactiveStacks.Count == 0)
            return null;

        int lastIndex = inactiveStacks.Count - 1;
        ChipStackView stackView = inactiveStacks[lastIndex];
        inactiveStacks.RemoveAt(lastIndex);
        return stackView;
    }

    private void DeactivateStack(ChipStackView stackView)
    {
        if (stackView == null)
            return;

        stackView.ResetView();
        stackView.gameObject.SetActive(false);

        if (!inactiveStacks.Contains(stackView))
            inactiveStacks.Add(stackView);
    }

    private void ReleasePendingChipReservation(RouletteBetArea betArea)
    {
        if (!pendingChipsByArea.TryGetValue(betArea, out int pendingCount))
            return;

        pendingCount--;

        if (pendingCount <= 0)
        {
            pendingChipsByArea.Remove(betArea);
            return;
        }

        pendingChipsByArea[betArea] = pendingCount;
    }

    public void RestoreActiveBets(IReadOnlyList<RouletteBet> activeBets, RouletteWheelType wheelType)
    {
        if (activeBets == null || activeBets.Count == 0)
        {
            ClearAll();
            return;
        }

        stackVisualVersion++;
        RouletteBetArea[] allAreas = FindObjectsByType<RouletteBetArea>();

        foreach (RouletteBet bet in activeBets)
        {
            if (bet == null)
                continue;

            ChipDenomination denomination = InferDenomination(bet.Stake);
            RouletteBetArea match = FindMatchingBetArea(bet, allAreas, wheelType);

            if (match == null)
            {
                Debug.LogWarning($"Could not find matching bet area for bet: {bet.Type} stake={bet.Stake}. Skipping visual restore.");
                continue;
            }

            ShowOrUpdateStack(match, denomination, bet.Stake, stackVisualVersion);
        }
    }

    private static ChipDenomination InferDenomination(int stake)
    {
        if (Enum.IsDefined(typeof(ChipDenomination), stake))
            return (ChipDenomination)stake;

        Debug.LogWarning($"Stake value {stake} does not match a known denomination. Using Chip250 as fallback.");
        return ChipDenomination.Chip250;
    }

    private static RouletteBetArea FindMatchingBetArea(RouletteBet savedBet, RouletteBetArea[] allAreas, RouletteWheelType wheelType)
    {
        foreach (RouletteBetArea area in allAreas)
        {
            if (area == null || !area.IsAvailableForWheelType(wheelType))
                continue;

            RouletteBet probeBet = area.CreateBet(savedBet.Stake, wheelType);

            if (BetsMatch(probeBet, savedBet))
                return area;
        }

        return null;
    }

    private static bool BetsMatch(RouletteBet first, RouletteBet second)
    {
        if (first == null || second == null)
            return false;

        if (first.Type != second.Type)
            return false;

        if (first.CoveredSlotIds.Count != second.CoveredSlotIds.Count)
            return false;

        for (int i = 0; i < first.CoveredSlotIds.Count; i++)
        {
            if (first.CoveredSlotIds[i] != second.CoveredSlotIds[i])
                return false;
        }

        return true;
    }
}
