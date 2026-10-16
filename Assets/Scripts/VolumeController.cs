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
        // スライダーの初期値をMixerから取得、または好みの初期値(例: 0.75f)に設定
        // ここでは仮に1.0(最大値)で初期化
        bgmSlider.value = 1.0f;
        seSlider.value = 1.0f;

        // スライダーが動いたときのイベントを登録
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolume);
    }

    public void SetBGMVolume(float volume)
    {
        // 0〜1の値をデシベル（-80dB 〜 0dB）に変換してMixerに適用
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSEVolume(float volume)
    {
        audioMixer.SetFloat("SEVolume", Mathf.Log10(volume) * 20);
    }
}
