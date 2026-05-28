using UnityEngine;

public class RouletteSoundManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Per-Event Volumes")]
    [SerializeField, Range(0f, 1f)] private float chipVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float spinVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float ballDropVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float winVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float loseVolume = 0.7f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] chipPlacementClips;
    [SerializeField] private AudioClip spinClip;
    [SerializeField] private AudioClip ballDropResultClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip musicLoopClip;

    [Header("Chip Sound Variation")]
    [SerializeField, Range(0.5f, 2f)] private float chipPitchMin = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float chipPitchMax = 1.05f;

    [Header("Scene References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private RouletteWheelSpinAnimator wheelSpinAnimator;

    private void Awake()
    {
        if (gameFlowController == null)
        {
            Debug.LogError($"{nameof(RouletteSoundManager)}: GameFlowController reference is missing.");
            return;
        }
    }

    private void Start()
    {
        PlayMusic();
    }

    private void OnEnable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnBetPlaced += HandleBetPlaced;
        gameFlowController.OnGameStateChanged += HandleGameStateChanged;
        gameFlowController.OnRoundResolved += HandleRoundResolved;

        if (wheelSpinAnimator != null)
            wheelSpinAnimator.OnBallDropStarted += HandleBallDropStarted;
    }

    private void OnDisable()
    {
        if (gameFlowController == null)
            return;

        gameFlowController.OnBetPlaced -= HandleBetPlaced;
        gameFlowController.OnGameStateChanged -= HandleGameStateChanged;
        gameFlowController.OnRoundResolved -= HandleRoundResolved;

        if (wheelSpinAnimator != null)
            wheelSpinAnimator.OnBallDropStarted -= HandleBallDropStarted;
    }

    private void HandleBetPlaced()
    {
        PlayChipSound();
    }

    private void HandleGameStateChanged()
    {
        if (gameFlowController.GameState.FlowState == GameFlowState.Spinning)
            PlaySpinSound();
    }

    private void HandleRoundResolved(RoundResult result)
    {
        if (result.HasAnyWinningBet)
            PlayWin();
        else
            PlayLose();
    }

    private void HandleBallDropStarted()
    {
        PlayBallDropSound();
    }

    private void PlayChipSound()
    {
        if (chipPlacementClips == null || chipPlacementClips.Length == 0)
            return;

        AudioClip clip = chipPlacementClips[Random.Range(0, chipPlacementClips.Length)];
        float pitch = Random.Range(chipPitchMin, chipPitchMax);
        float volume = masterVolume * sfxVolume * chipVolume;

        PlayOneShot(clip, volume, pitch);
    }

    private void PlaySpinSound()
    {
        if (spinClip == null)
            return;

        float volume = masterVolume * sfxVolume * spinVolume;
        PlayOneShot(spinClip, volume);
    }

    private void PlayBallDropSound()
    {
        if (ballDropResultClip == null)
            return;

        float volume = masterVolume * sfxVolume * ballDropVolume;
        PlayOneShot(ballDropResultClip, volume);
    }

    private void PlayWin()
    {
        if (winClip == null)
            return;

        float volume = masterVolume * sfxVolume * winVolume;
        PlayOneShot(winClip, volume);
    }

    private void PlayLose()
    {
        if (loseClip == null)
            return;

        float volume = masterVolume * sfxVolume * loseVolume;
        PlayOneShot(loseClip, volume);
    }

    private void PlayMusic()
    {
        if (musicSource == null || musicLoopClip == null)
            return;

        musicSource.volume = masterVolume * musicVolume;
        musicSource.clip = musicLoopClip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlayOneShot(AudioClip clip, float volume, float pitch = 1f)
    {
        if (sfxSource == null || clip == null)
            return;

        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = originalPitch;
    }
}
