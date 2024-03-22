using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleAudio : MonoBehaviour
{
    [SerializeField]
    public Sprite mutedSprite;

    [SerializeField]
    public Sprite unmutedSprite;

    [SerializeField]
    private Slider volumeSlider;

    private float unmutedVolume;

    private MMSoundManager soundManager;

    private Image muteButtonImage;

    private const float SFX_VOLUME = 2f;
    private const float MASTER_VOLUME = 0.7f;
    private const float MUSIC_VOLUME = 0.5f;

    //The engines defines this value as 0 (muted)
    private const float MUTED_VOLUME = 0.0001f;

    [SerializeField]
    private MMSoundManager.MMSoundManagerTracks channel;

    [SerializeField]
    TextMeshProUGUI textSoundState;
    string offState = "OFF";
    string onState = "ON";

    void Start()
    {
        muteButtonImage = GetComponent<Image>();
        soundManager = MMSoundManager.Instance;
        soundManager.SetTrackVolume(MMSoundManager.MMSoundManagerTracks.Master, MASTER_VOLUME);
        SetUnmutedVolume();
        soundManager.SetVolumeSfx(SFX_VOLUME);
        if (IsMuted(channel))
        {
            UpdateMutedUIState();
        }
        else
        {
            UpdateUnmutedUIState();
        }
    }

    void Update()
    {
        if (
            volumeSlider
            && (IsMuted(channel) && unmutedVolume != volumeSlider.value)
            && volumeSlider.value > MUTED_VOLUME
        )
        {
            unmutedVolume = volumeSlider.value;
            UpdateUnmutedUIState();
        }
        if (volumeSlider.value == 0)
        {
            UpdateMutedUIState();
        }
    }

    public void SetUnmutedVolume()
    {
        if (!IsMuted(channel))
        {
            float currentMusicVolume = soundManager.GetTrackVolume(
                MMSoundManager.MMSoundManagerTracks.Music,
                false
            );
            float musicVolume =
                currentMusicVolume != MUSIC_VOLUME ? currentMusicVolume : MUSIC_VOLUME;
            unmutedVolume = volumeSlider ? volumeSlider.value : musicVolume;
        }
        else
        {
            unmutedVolume = volumeSlider ? volumeSlider.value : MUSIC_VOLUME;
        }
    }

    public void Toggle()
    {
        if (IsMuted(channel))
        {
            PlaySound();
            UpdateUnmutedUIState();
        }
        else
        {
            SilenceSound();
            UpdateMutedUIState();
        }
    }

    private void SilenceSound()
    {
        SetUnmutedVolume();
        switch (channel)
        {
            case MMSoundManager.MMSoundManagerTracks.Music:
                soundManager.MuteMusic();
                break;
            case MMSoundManager.MMSoundManagerTracks.Sfx:
                soundManager.MuteSfx();
                break;
        }
    }

    private void PlaySound()
    {
        switch (channel)
        {
            case MMSoundManager.MMSoundManagerTracks.Music:
                soundManager.UnmuteMusic();
                break;
            case MMSoundManager.MMSoundManagerTracks.Sfx:
                soundManager.UnmuteSfx();
                break;
        }
        SetVolume(unmutedVolume);
    }

    private void SetVolume(float newVolume)
    {
        switch (channel)
        {
            case MMSoundManager.MMSoundManagerTracks.Music:
                soundManager.SetVolumeMusic(newVolume);
                break;
            case MMSoundManager.MMSoundManagerTracks.Sfx:
                soundManager.SetVolumeSfx(newVolume);
                break;
        }
    }

    private bool IsMuted(MMSoundManager.MMSoundManagerTracks track)
    {
        // This may seem wrong, but it's not. The IsMuted() method does exactly the opposite of what its name suggests.
        return !soundManager.IsMuted(track)
            || soundManager.GetTrackVolume(track, false) <= MUTED_VOLUME;
    }

    private void UpdateUnmutedUIState()
    {
        muteButtonImage.enabled = unmutedSprite != null;
        muteButtonImage.overrideSprite = unmutedSprite;
        textSoundState.text = onState;
    }

    private void UpdateMutedUIState()
    {
        muteButtonImage.enabled = mutedSprite != null;
        muteButtonImage.overrideSprite = mutedSprite;
        textSoundState.text = offState;
    }
}
