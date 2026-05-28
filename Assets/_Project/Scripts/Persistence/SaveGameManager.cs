using System;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipSelectionController chipSelectionController;
    [SerializeField] private ChipStackViewController chipStackViewController;

    [Header("Settings")]
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private bool logSaveOperations = true;

    private bool hasRestored;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        TryLoadFromDisk();
    }

    private void OnEnable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnGameStateChanged += HandleGameStateChanged;
        gameFlowController.OnRoundResolved += HandleRoundResolved;
        gameFlowController.OnBetsCleared += HandleBetsCleared;
        gameFlowController.OnBetPlaced += HandleBetPlaced;
    }

    private void OnDisable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnGameStateChanged -= HandleGameStateChanged;
        gameFlowController.OnRoundResolved -= HandleRoundResolved;
        gameFlowController.OnBetsCleared -= HandleBetsCleared;
        gameFlowController.OnBetPlaced -= HandleBetPlaced;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void HandleBetPlaced()
    {
        AutoSave();
    }

    private void HandleGameStateChanged()
    {
        if (gameFlowController != null &&
            gameFlowController.GameState != null &&
            gameFlowController.GameState.FlowState == GameFlowState.Betting &&
            hasRestored)
        {
            AutoSave();
        }
    }

    private void HandleRoundResolved(RoundResult result)
    {
    }

    private void HandleBetsCleared()
    {
        AutoSave();
    }

    private void AutoSave()
    {
        if (!autoSaveEnabled)
            return;

        if (gameFlowController == null || gameFlowController.GameState == null)
            return;

        if (gameFlowController.GameState.FlowState != GameFlowState.Betting)
            return;

        Save();
    }

    public void Save()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
        {
            Debug.Log("[SaveGameManager] Save skipped: GameFlowController or GameState not available.");
            return;
        }

        try
        {
            SaveGameData data = gameFlowController.ExportSaveData();

            if (chipSelectionController != null)
                data.selectedChipValue = chipSelectionController.ExportSelectedChipValue();

            SaveGameRepository.Save(data);

            if (logSaveOperations)
                Debug.Log("[SaveGameManager] Save completed.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveGameManager: Save failed with exception: {exception.Message}");
        }
    }

    private void TryLoadFromDisk()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
        {
            Debug.Log("[SaveGameManager] Load skipped: GameFlowController not available.");
            return;
        }

        if (!SaveGameRepository.SaveFileExists())
        {
            Debug.Log("[SaveGameManager] No save file found. Starting fresh game.");
            return;
        }

        try
        {
            SaveGameData saveData = SaveGameRepository.Load();

            if (saveData == null)
            {
                Debug.LogWarning("[SaveGameManager] Save file exists but could not be loaded. Starting fresh game.");
                SaveGameRepository.DeleteSave();
                return;
            }

            if (saveData.version != 1)
            {
                Debug.LogWarning($"[SaveGameManager] Unknown save version: {saveData.version}. Starting fresh game.");
                SaveGameRepository.DeleteSave();
                return;
            }

            if (saveData.gameState == null)
            {
                Debug.LogWarning("[SaveGameManager] Save data has no game state. Starting fresh game.");
                SaveGameRepository.DeleteSave();
                return;
            }

            gameFlowController.RestoreFromSave(saveData);
            gameFlowController.RefreshStateViews();

            if (chipSelectionController != null)
                chipSelectionController.RestoreSelectedChip(saveData.selectedChipValue);

            if (chipStackViewController != null)
                chipStackViewController.RestoreActiveBets(
                    gameFlowController.GetActiveBetsForSnapshot(),
                    gameFlowController.GameState.WheelType);

            hasRestored = true;
            Debug.Log("[SaveGameManager] Game state restored from save file successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveGameManager: Load failed with exception: {exception.Message}. " +
                           "Falling back to default game state.");

            gameFlowController.ClearSaveData();
            gameFlowController.RefreshStateViews();

            if (chipSelectionController != null)
                chipSelectionController.SelectDefaultChip();

            SaveGameRepository.DeleteSave();
            Debug.Log("[SaveGameManager] Game started with default state after corrupt save.");
        }
    }

    private void ValidateReferences()
    {
        if (gameFlowController == null)
            Debug.LogError($"{nameof(SaveGameManager)} needs a GameFlowController reference.");
    }

}
