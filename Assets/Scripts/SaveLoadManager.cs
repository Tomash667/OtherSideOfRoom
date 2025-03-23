using UnityEngine;
using System.IO;

public class SaveLoadManager : MonoBehaviour
{
    private static string savePath => Application.persistentDataPath + "/gameData.json";

    public static void Save(GameData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(savePath, json);
    }

    public static GameData Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<GameData>(json);
        }
        return new GameData(); // Return default data if no save exists
    }
}
