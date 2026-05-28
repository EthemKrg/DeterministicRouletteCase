using System.IO;
using UnityEngine;

public static class SaveGameRepository
{
    private static readonly string FileName = "deterministic_roulette_save.json";
    private static readonly string TempFileName = "deterministic_roulette_save.tmp";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string TempFilePath => Path.Combine(Application.persistentDataPath, TempFileName);

    public static void Save(SaveGameData data)
    {
        if (data == null)
        {
            Debug.LogError("SaveGameRepository: Cannot save null data.");
            return;
        }

        string json = JsonUtility.ToJson(data);

        try
        {
            string directory = Application.persistentDataPath;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(TempFilePath, json);

            if (File.Exists(FilePath))
                File.Delete(FilePath);

            File.Move(TempFilePath, FilePath);

            //Debug.Log($"SaveGameRepository: Save completed ({FilePath})");
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"SaveGameRepository: Save failed: {exception.Message}");

            if (File.Exists(TempFilePath))
                File.Delete(TempFilePath);
        }
    }

    public static SaveGameData Load()
    {
        if (!File.Exists(FilePath))
        {
            Debug.Log("SaveGameRepository: No save file found.");
            return null;
        }

        if (File.Exists(TempFilePath))
            File.Delete(TempFilePath);

        try
        {
            string json = File.ReadAllText(FilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("SaveGameRepository: Save file is empty.");
                return null;
            }

            SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);

            if (data == null)
            {
                Debug.LogWarning("SaveGameRepository: Failed to deserialize save data.");
                return null;
            }

            return data;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"SaveGameRepository: Load failed: {exception.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);

        if (File.Exists(TempFilePath))
            File.Delete(TempFilePath);

        Debug.Log("SaveGameRepository: Save file deleted.");
    }

    public static bool SaveFileExists()
    {
        return File.Exists(FilePath);
    }
}
