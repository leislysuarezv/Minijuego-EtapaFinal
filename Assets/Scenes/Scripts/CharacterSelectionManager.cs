using UnityEngine;

public static class CharacterSelectionManager
{
    public const string SelectedCharacterKey = "SelectedCharacterIndex";
    private const string UseSavedSelectionOnceKey = "CircuitPathUseSavedSelectionOnce";

    private static bool hasSessionSelection;
    private static int selectedCharacterIndex;
    private static bool useSavedSelectionOnNextLoad;
    private static readonly Sprite[] previewSprites = new Sprite[2];

    public static bool HasSessionSelection
    {
        get { return hasSessionSelection; }
    }

    public static int SelectedCharacterIndex
    {
        get { return selectedCharacterIndex; }
    }

    public static int RivalCharacterIndex
    {
        get { return selectedCharacterIndex == 0 ? 1 : 0; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetSessionSelection()
    {
        // The player must choose a character before gameplay begins in each fresh scene load.
        selectedCharacterIndex = PlayerPrefs.GetInt(SelectedCharacterKey, 0);
        hasSessionSelection = useSavedSelectionOnNextLoad || PlayerPrefs.GetInt(UseSavedSelectionOnceKey, 0) == 1;
        useSavedSelectionOnNextLoad = false;

        if (hasSessionSelection)
        {
            PlayerPrefs.DeleteKey(UseSavedSelectionOnceKey);
            PlayerPrefs.Save();
        }
    }

    public static void SaveSelection(int characterIndex)
    {
        // PlayerPrefs keeps the last choice available for future runs while the session flag unlocks this scene.
        selectedCharacterIndex = Mathf.Clamp(characterIndex, 0, 1);
        hasSessionSelection = true;
        PlayerPrefs.SetInt(SelectedCharacterKey, selectedCharacterIndex);
        PlayerPrefs.Save();
    }

    public static void ClearSessionSelection()
    {
        // Returning to the main menu should force the player to choose again before gameplay starts.
        hasSessionSelection = false;
        useSavedSelectionOnNextLoad = false;
        PlayerPrefs.DeleteKey(UseSavedSelectionOnceKey);
        PlayerPrefs.Save();
        selectedCharacterIndex = PlayerPrefs.GetInt(SelectedCharacterKey, 0);
    }

    public static void UseSavedSelectionOnNextSceneLoad()
    {
        useSavedSelectionOnNextLoad = true;
        PlayerPrefs.SetInt(UseSavedSelectionOnceKey, 1);
        PlayerPrefs.Save();
    }

    public static void SetPreviewSprite(int characterIndex, Sprite sprite)
    {
        if (characterIndex < 0 || characterIndex >= previewSprites.Length || sprite == null)
        {
            return;
        }

        previewSprites[characterIndex] = sprite;
    }

    public static Sprite GetPreviewSprite(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= previewSprites.Length)
        {
            return null;
        }

        return previewSprites[characterIndex];
    }
}
