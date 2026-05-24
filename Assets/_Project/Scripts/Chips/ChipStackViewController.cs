using System.Collections.Generic;
using UnityEngine;

public class ChipStackViewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipStackView chipStackPrefab;
    [SerializeField] private Transform stackRoot;

    [Header("Placement")]
    [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.08f, 0f);

    private readonly Dictionary<RouletteBetArea, ChipStackView> stacksByArea = new Dictionary<RouletteBetArea, ChipStackView>();
    private readonly Dictionary<RouletteBetArea, int> stakesByArea = new Dictionary<RouletteBetArea, int>();

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
        {
            gameFlowController.OnRoundResolved += HandleRoundResolved;
            gameFlowController.OnBetsCleared += HandleBetsCleared;
        }
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
        {
            gameFlowController.OnRoundResolved -= HandleRoundResolved;
            gameFlowController.OnBetsCleared -= HandleBetsCleared;
        }
    }

    public void ShowOrUpdateStack(RouletteBetArea betArea, ChipDenomination denomination, int addedStake)
    {
        if (betArea == null)
            return;

        if (addedStake <= 0)
            return;

        if (!stakesByArea.ContainsKey(betArea))
            stakesByArea[betArea] = 0;

        stakesByArea[betArea] += addedStake;

        ChipStackView stackView = GetOrCreateStack(betArea);
        stackView.AddChip(denomination);
        stackView.SetStake(stakesByArea[betArea]);
    }

    public void ClearAll()
    {
        foreach (ChipStackView stackView in stacksByArea.Values)
        {
            if (stackView != null)
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
            betArea.transform.position + stackOffset,
            Quaternion.identity,
            root);

        stackView.DisableColliders();

        stacksByArea[betArea] = stackView;

        return stackView;
    }

    private void HandleRoundResolved(RoundResult result)
    {
        ClearAll();
    }

    private void ValidateReferences()
    {
        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(ChipStackViewController)} needs a GameFlowController reference.");

        if (chipStackPrefab == null)
            throw new System.InvalidOperationException($"{nameof(ChipStackViewController)} needs a chip stack prefab reference.");
    }

    private void HandleBetsCleared()
    {
        ClearAll();
    }
}