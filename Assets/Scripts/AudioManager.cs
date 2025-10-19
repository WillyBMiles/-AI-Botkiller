using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Player Movement Sounds")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip slideJumpSound;
    [SerializeField] private AudioClip slideLoopSound;
    
    [Header("Combat Sounds")]
    [SerializeField] private AudioClip playerShootSound;
    [SerializeField] private AudioClip enemyShootSound;
    [SerializeField] private AudioClip enemyGotShotSound;
    [SerializeField] private AudioClip enemyDieSound;
    [SerializeField] private AudioClip playerGotShotSound;
    
    [Header("Scoring Sounds")]
    [SerializeField] private AudioClip comboMultiplierIncreaseSound;
    [SerializeField] private AudioClip trickCompletedSound;
    [SerializeField] private AudioClip comboFinishSound;
    
    [Header("Pickup Sounds")]
    [SerializeField] private AudioClip ammoPickupSound;
    
    [Header("Environment Sounds")]
    [SerializeField] private AudioClip bouncePadSound;
    
    [Header("Game State Sounds")]
    [SerializeField] private AudioClip playerLoseSound;
    
    // Active looping audio sources
    private AudioSource slideAudioSource;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Plays a sound at a specific world position
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    /// <param name="position">World position to play the sound at</param>
    /// <param name="volume">Volume multiplier (0-1)</param>
    public static void PlaySound(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null audio clip");
            return;
        }
        
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
    
    /// <summary>
    /// Plays a sound at a specific world position with pitch variation
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    /// <param name="position">World position to play the sound at</param>
    /// <param name="volume">Volume multiplier (0-1)</param>
    /// <param name="pitchMin">Minimum pitch variation</param>
    /// <param name="pitchMax">Maximum pitch variation</param>
    private static void PlaySoundWithPitch(AudioClip clip, Vector3 position, float volume = 1f, float pitchMin = 0.9f, float pitchMax = 1.1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null audio clip");
            return;
        }
        
        // Create temporary GameObject for audio source with pitch control
        GameObject tempAudio = new GameObject("TempAudio");
        tempAudio.transform.position = position;
        
        AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.Play();
        
        // Destroy after clip finishes
        Destroy(tempAudio, clip.length / audioSource.pitch);
    }
    
    // Convenience methods for specific sounds (with 20% pitch variation)
    public static void PlayJumpSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.jumpSound, position, 0.4f, 0.8f, 1.2f);
    }
    
    public static void PlayDoubleJumpSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.doubleJumpSound, position, 0.4f, 0.8f, 1.2f);
    }
    
    public static void PlaySlideJumpSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.slideJumpSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayPlayerShootSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.playerShootSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayEnemyShootSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.enemyShootSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayEnemyGotShotSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.enemyGotShotSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayEnemyDieSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.enemyDieSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayPlayerGotShotSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.playerGotShotSound, position, 0.5f, 0.8f, 1.2f);
    }
    
    public static void PlayComboMultiplierIncreaseSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.comboMultiplierIncreaseSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayTrickCompletedSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.trickCompletedSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayComboFinishSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.comboFinishSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayPlayerLoseSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.playerLoseSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayAmmoPickupSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.ammoPickupSound, position, 1f, 0.8f, 1.2f);
    }
    
    public static void PlayBouncePadSound(Vector3 position)
    {
        if (Instance != null) PlaySoundWithPitch(Instance.bouncePadSound, position, 1f, 0.8f, 1.2f);
    }
    
    /// <summary>
    /// Starts playing the slide loop sound with fade in
    /// </summary>
    public static void StartSlideSound(Transform followTransform, float fadeInDuration = 0.2f)
    {
        if (Instance == null || Instance.slideLoopSound == null) return;
        
        // Stop any existing slide sound immediately
        if (Instance.slideAudioSource != null)
        {
            Instance.StopAllCoroutines();
            Destroy(Instance.slideAudioSource.gameObject);
            Instance.slideAudioSource = null;
        }
        
        // Create audio source for slide loop
        GameObject slideAudioObj = new GameObject("SlideAudio");
        slideAudioObj.transform.SetParent(followTransform);
        slideAudioObj.transform.localPosition = Vector3.zero;
        
        Instance.slideAudioSource = slideAudioObj.AddComponent<AudioSource>();
        Instance.slideAudioSource.clip = Instance.slideLoopSound;
        Instance.slideAudioSource.loop = true;
        Instance.slideAudioSource.spatialBlend = 1f; // 3D sound
        Instance.slideAudioSource.volume = 0f; // Start at 0 for fade in
        Instance.slideAudioSource.Play();
        
        // Start fade in coroutine
        Instance.StartCoroutine(Instance.FadeSlideSound(Instance.slideAudioSource, 0f, 1f, fadeInDuration));
    }
    
    /// <summary>
    /// Stops the slide loop sound with fade out
    /// </summary>
    public static void StopSlideSound(float fadeOutDuration = 0.2f)
    {
        if (Instance == null || Instance.slideAudioSource == null) return;
        
        // Capture current audio source reference before starting coroutine
        AudioSource audioToFade = Instance.slideAudioSource;
        Instance.slideAudioSource = null; // Clear reference immediately to prevent conflicts
        
        // Start fade out coroutine with captured reference
        Instance.StartCoroutine(Instance.FadeSlideSound(audioToFade, audioToFade.volume, 0f, fadeOutDuration));
    }
    
    private System.Collections.IEnumerator FadeSlideSound(AudioSource audioSource, float startVolume, float targetVolume, float duration)
    {
        if (audioSource == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < duration && audioSource != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        if (audioSource != null)
        {
            audioSource.volume = targetVolume;
            
            // If fading out to 0, destroy the audio source
            if (targetVolume <= 0f)
            {
                Destroy(audioSource.gameObject);
            }
        }
    }
}
