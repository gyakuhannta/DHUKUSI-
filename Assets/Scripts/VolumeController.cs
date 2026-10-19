using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    private void Start()
    {
        // 1. ゲーム起動時に現在のAudioMixerの音量をスライダーに反映させる
        if (audioMixer.GetFloat("BGMVolume", out float currentBgmDb))
        {
            // デシベル（-80dB 〜 0dB）から 0 〜 1 の値に逆算してスライダーに代入
            bgmSlider.value = Mathf.Pow(10, currentBgmDb / 20f);
        }
        else
        {
            bgmSlider.value = 1.0f; // 取得失敗時のフォールバック
        }

        if (audioMixer.GetFloat("SEVolume", out float currentSeDb))
        {
            seSlider.value = Mathf.Pow(10, currentSeDb / 20f);
        }
        else
        {
            seSlider.value = 1.0f;
        }

        // スライダーが動いたときのイベントを登録
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolume);
    }

    public void SetBGMVolume(float volume)
    {
        // 2. volumeが0のときに-Infinityになるのを防ぐため、最小値を0.0001fに制限
        float clampedVolume = Mathf.Clamp(volume, 0.0001f, 1.0f);
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(clampedVolume) * 20);
    }

    public void SetSEVolume(float volume)
    {
        // 同様に最小値を制限
        float clampedVolume = Mathf.Clamp(volume, 0.0001f, 1.0f);
        audioMixer.SetFloat("SEVolume", Mathf.Log10(clampedVolume) * 20);
    }
}
