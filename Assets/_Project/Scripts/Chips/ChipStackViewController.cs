using System.Collections.Generic;
using UnityEngine;

public class ChipStackViewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipStackView chipStackPrefab;
    [SerializeField] private ChipVisualPool chipVisualPool;
    [SerializeField] private BalanceChipDisplayController balanceChipDisplayController;
    [SerializeField] private Transform stackRoot;

    [Header("Placement")]
    [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.08f, 0f);

    private readonly Dictionary<RouletteBetArea, ChipStackView> stacksByArea = new Dictionary<RouletteBetArea, ChipStackView>();
    private readonly Dictionary<RouletteBetArea, int> stakesByArea = new Dictionary<RouletteBetArea, int>();

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

        ChipStackView stackView = GetOrCreateStack(betArea);
        stackView.AddChip(denomination, stakesByArea[betArea]);
    }

    public Vector3 GetStackPosition(RouletteBetArea betArea)
    {
        return betArea.transform.position + stackOffset;
    }

    public Vector3 GetNextChipWorldPosition(RouletteBetArea betArea)
    {
        if (stacksByArea.TryGetValue(betArea, out ChipStackView stackView) && stackView != null)
            return stackView.GetNextChipWorldPosition();

        return GetStackPosition(betArea);
    }

    public int GetVisualVersion()
    {
        return stackVisualVersion;
    }

    public void ClearAll()
    {
        stackVisualVersion++;

        foreach (ChipStackView stackView in stacksByArea.Values)
        {
            if (stackView == null)
                continue;

            stackView.Clear();
            Destroy(stackView.gameObject);
        }

        stacksByArea.Clear();
        stakesByArea.Clear();
    }

    private ChipStackView GetOrCreateStack(RouletteBetArea betArea)
    {
        if (stacksByArea.TryGetValue(betArea, out ChipStackView existingStack) && existingStack != null)
            return existingStack;

        Transform root = stackRoot != null ? stackRoot : transform;

        ChipStackView stackView = Instantiate(
            chipStackPrefab,
            GetStackPosition(betArea),
            Quaternion.identity,
            root);

        stackView.Initialize(chipVisualPool);

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
                Destroy(stackView.gameObject);
            }

            stacksByArea.Clear();
            stakesByArea.Clear();
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
}
