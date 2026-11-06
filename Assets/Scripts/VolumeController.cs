using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    // ミキサーのパラメータ名と保存用のキー
    private const string BGM_KEY = "BGMVolume";
    private const string SE_KEY = "SEVolume";

    // 最小音量の規定値（完全に0にはせず、この値未満にならないようにする）
    private const float MIN_VOLUME = 0.0001f;

    private void Start()
    {
        // 1. スライダーのイベント登録
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        seSlider.onValueChanged.AddListener(SetSEVolume);

        // 2. 前回の保存値（なければ初期値1.0）をスライダーに適用
        // もし保存された値が最小値未満なら最小値にする
        float savedBGM = PlayerPrefs.GetFloat(BGM_KEY, 1.0f);
        bgmSlider.value = Mathf.Max(savedBGM, MIN_VOLUME);

        float savedSE = PlayerPrefs.GetFloat(SE_KEY, 1.0f);
        seSlider.value = Mathf.Max(savedSE, MIN_VOLUME);

        // 3. 起動直後のAudioMixer初期化待ち対策として、1フレーム遅らせてミキサーに反映
        StartCoroutine(ApplyInitialVolumeCoroutine());
    }

    private IEnumerator ApplyInitialVolumeCoroutine()
    {
        yield return null;

        // スライダーの現在の値をミキサーに即座に反映させる
        SetBGMVolume(bgmSlider.value);
        SetSEVolume(seSlider.value);
    }

    public void SetBGMVolume(float volume)
    {
        // 0以下にならないように下限を制限
        if (volume < MIN_VOLUME)
        {
            volume = MIN_VOLUME;
            bgmSlider.value = MIN_VOLUME; // スライダーの見た目も0.0001で止める
        }

        float clampedVolume = Mathf.Clamp(volume, MIN_VOLUME, 1.0f);
        audioMixer.SetFloat(BGM_KEY, Mathf.Log10(clampedVolume) * 20);

        // 音量を端末に保存
        PlayerPrefs.SetFloat(BGM_KEY, volume);
    }

    public void SetSEVolume(float volume)
    {
        // 0以下にならないように下限を制限
        if (volume < MIN_VOLUME)
        {
            volume = MIN_VOLUME;
            seSlider.value = MIN_VOLUME; // スライダーの見た目も0.0001で止める
        }

        float clampedVolume = Mathf.Clamp(volume, MIN_VOLUME, 1.0f);
        audioMixer.SetFloat(SE_KEY, Mathf.Log10(clampedVolume) * 20);

        // 音量を端末に保存
        PlayerPrefs.SetFloat(SE_KEY, volume);
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
        if (seSlider != null) seSlider.onValueChanged.RemoveListener(SetSEVolume);
    }
}
