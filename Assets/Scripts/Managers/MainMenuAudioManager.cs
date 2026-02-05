using UnityEngine;

public class MainMenuAudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip menuBGM;
    public AudioClip playNowBGM;

    [Header("Settings")]
    [Tooltip("Check this if you want the music to continue playing when the scene changes.")]
    public bool persistOnSceneLoad = true;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("MainMenuAudioManager: AudioSource Missing! Add an AudioSource component.");
            return;
        }

        if (menuBGM == null)
        {
            Debug.LogWarning("MainMenuAudioManager: Menu BGM is not assigned!");
        }

        // Play Menu BGM
        if (audioSource != null && menuBGM != null)
        {
            Debug.Log($"MainMenuAudioManager: Attempting to play {menuBGM.name}");
            if (audioSource.clip != menuBGM)
            {
                audioSource.clip = menuBGM;
                audioSource.loop = true;
                
                audioSource.volume = 1f;
                audioSource.spatialBlend = 0f;
                audioSource.mute = false;
                audioSource.enabled = true; // Force Component on
                audioSource.ignoreListenerPause = true; // Keep playing even if game is paused
                
                audioSource.Play();
            }
        }
    }

    void Update()
    {
        // Debugging: Monitor playback status
        if (audioSource != null && menuBGM != null && !audioSource.isPlaying)
        {
             // Only log this once in a while to avoid spam, or check if it stopped unexpectedly
             // Debug.LogWarning("MainMenuAudioManager: AudioSource is NOT playing! (Did it finish? Or stopped?)");
        }
    }

    /// <summary>
    /// Call this method via the Button's OnClick event.
    /// It changes the music to the 'Play Now' BGM.
    /// </summary>
    public void ChangeToPlayNowMusic()
    {
        Debug.Log("MainMenuAudioManager: PlayButton Clicked");
        if (audioSource != null && playNowBGM != null)
        {
            // Only switch if it's not already playing
            if (audioSource.clip != playNowBGM)
            {
                Debug.Log($"MainMenuAudioManager: Switching to {playNowBGM.name}");
                audioSource.Stop();
                audioSource.clip = playNowBGM;
                audioSource.loop = true; 
                
                // Force settings to ensure it's audible
                audioSource.volume = 1f;
                audioSource.spatialBlend = 0f; // 2D Sound
                audioSource.mute = false;
                audioSource.enabled = true;
                audioSource.ignoreListenerPause = true;
                
                audioSource.Play();
            }

            if (persistOnSceneLoad)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }
        else
        {
             Debug.LogError("MainMenuAudioManager: Source or PlayNowBGM missing!");
        }
    }
}
