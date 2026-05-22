using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectedCharacterSpawner : MonoBehaviour
{
    private const float AlyaRoleScale = 0.41423255f;
    private const float BobRoleScale = 0.97f;
    private static readonly Vector2 OriginalPlayerColliderSize = new Vector2(6.07f, 15.4f);
    private static readonly string[] SecondCharacterFrameResources =
    {
        "p1",
        "p2",
        "p3"
    };

    [SerializeField] private GameObject[] sceneCharacterObjects;
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform spawnPoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SpawnRuntimeApplier();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnRuntimeApplier();
    }

    private static void SpawnRuntimeApplier()
    {
        if (FindObjectOfType<SelectedCharacterSpawner>() != null)
        {
            return;
        }

        GameObject applierObject = new GameObject(nameof(SelectedCharacterSpawner));
        applierObject.AddComponent<SelectedCharacterSpawner>();
    }

    private IEnumerator Start()
    {
        // Wait for the selection UI before touching gameplay objects.
        while (!CharacterSelectionManager.HasSessionSelection)
        {
            yield return null;
        }

        ApplySelectedCharacterToCurrentScene();
    }

    public static void ApplySelectedCharacterToCurrentScene()
    {
        SelectedCharacterSpawner existingSpawner = FindObjectOfType<SelectedCharacterSpawner>();

        if (existingSpawner != null)
        {
            existingSpawner.ApplySelection();
            return;
        }

        GameObject applierObject = new GameObject(nameof(SelectedCharacterSpawner));
        existingSpawner = applierObject.AddComponent<SelectedCharacterSpawner>();
        existingSpawner.ApplySelection();
    }

    private void ApplySelection()
    {
        int selectedIndex = CharacterSelectionManager.SelectedCharacterIndex;

        if (TryApplySceneCharacters(selectedIndex))
        {
            return;
        }

        if (TrySpawnPrefab(selectedIndex))
        {
            return;
        }

        ApplySelectionToExistingPlayer(selectedIndex);
    }

    private bool TryApplySceneCharacters(int selectedIndex)
    {
        if (sceneCharacterObjects == null || sceneCharacterObjects.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < sceneCharacterObjects.Length; i++)
        {
            if (sceneCharacterObjects[i] != null)
            {
                sceneCharacterObjects[i].SetActive(i == selectedIndex);
            }
        }

        return true;
    }

    private bool TrySpawnPrefab(int selectedIndex)
    {
        if (characterPrefabs == null || selectedIndex < 0 || selectedIndex >= characterPrefabs.Length || characterPrefabs[selectedIndex] == null)
        {
            return false;
        }

        Transform targetSpawnPoint = spawnPoint != null ? spawnPoint : FindPlayerTransform();
        Vector3 spawnPosition = targetSpawnPoint != null ? targetSpawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = targetSpawnPoint != null ? targetSpawnPoint.rotation : Quaternion.identity;
        Instantiate(characterPrefabs[selectedIndex], spawnPosition, spawnRotation);
        return true;
    }

    private void ApplySelectionToExistingPlayer(int selectedIndex)
    {
        GameObject playerObject = FindPlayerObject();

        if (playerObject == null)
        {
            return;
        }

        if (selectedIndex == 0)
        {
            // Character 0 keeps the current scene player, AnimatorController, collider, line renderer, and movement scripts.
            SpriteRenderer alyaSpriteRenderer = playerObject.GetComponent<SpriteRenderer>();
            Sprite alyaSprite = CharacterSelectionManager.GetPreviewSprite(0);

            if (alyaSpriteRenderer != null && alyaSprite != null)
            {
                alyaSpriteRenderer.sprite = alyaSprite;
            }

            Animator alyaAnimator = playerObject.GetComponent<Animator>();

            if (alyaAnimator != null)
            {
                alyaAnimator.enabled = false;
            }

            ApplyPlayerRoleScale(playerObject, AlyaRoleScale);

            PlayerFollowMouse playerMovement = playerObject.GetComponent<PlayerFollowMouse>();

            if (playerMovement != null)
            {
                playerMovement.enableAnimatorOnMove = true;
            }

            SelectedCharacterSpriteAnimator existingSpriteAnimator = playerObject.GetComponent<SelectedCharacterSpriteAnimator>();

            if (existingSpriteAnimator != null)
            {
                existingSpriteAnimator.enabled = false;
            }

            return;
        }

        // Character 1 reuses the same gameplay object and swaps the visual frames to avoid changing controllers/colliders.
        Sprite[] secondCharacterFrames = LoadSecondCharacterFrames();
        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && secondCharacterFrames.Length > 0)
        {
            spriteRenderer.sprite = secondCharacterFrames[0];
        }

        ApplyPlayerRoleScale(playerObject, BobRoleScale);

        Animator animator = playerObject.GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        PlayerFollowMouse movement = playerObject.GetComponent<PlayerFollowMouse>();

        if (movement != null)
        {
            movement.enableAnimatorOnMove = false;
        }

        SelectedCharacterSpriteAnimator spriteAnimator = playerObject.GetComponent<SelectedCharacterSpriteAnimator>();

        if (spriteAnimator == null)
        {
            spriteAnimator = playerObject.AddComponent<SelectedCharacterSpriteAnimator>();
        }

        spriteAnimator.Configure(secondCharacterFrames);
    }

    private void ApplyPlayerRoleScale(GameObject playerObject, float visualScale)
    {
        // Keep the role size consistent while preserving the gameplay collider's world size.
        playerObject.transform.localScale = Vector3.one * visualScale;

        BoxCollider2D collider = playerObject.GetComponent<BoxCollider2D>();

        if (collider != null)
        {
            float colliderCompensation = AlyaRoleScale / visualScale;
            collider.size = OriginalPlayerColliderSize * colliderCompensation;
        }
    }

    private Transform FindPlayerTransform()
    {
        GameObject playerObject = FindPlayerObject();
        return playerObject != null ? playerObject.transform : null;
    }

    private GameObject FindPlayerObject()
    {
        // The scene also has a camera tagged as Player, so use the movement component to find the real playable character.
        PlayerFollowMouse playerMovement = FindObjectOfType<PlayerFollowMouse>();
        return playerMovement != null ? playerMovement.gameObject : null;
    }

    private Sprite[] LoadSecondCharacterFrames()
    {
        Sprite[] frames = new Sprite[SecondCharacterFrameResources.Length];

        for (int i = 0; i < SecondCharacterFrameResources.Length; i++)
        {
            frames[i] = Resources.Load<Sprite>(SecondCharacterFrameResources[i]);
        }

        return frames;
    }
}
