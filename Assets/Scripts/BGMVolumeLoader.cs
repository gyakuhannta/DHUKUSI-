using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class BGMVolumeLoader : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    // VolumeController（スライダー側）と同じパラメータ名とキー
    private const string BGM_KEY = "BGMVolume";
    private const string SE_KEY = "SEVolume";
    private const float MIN_VOLUME = 0.0001f;

    private void Start()
    {
        // 新しいシーンが始まった直後はミキサーが眠っているので、1フレーム遅らせてロードする
        StartCoroutine(LoadVolumeCoroutine());
    }

    private IEnumerator LoadVolumeCoroutine()
    {
        // 1フレーム待つ（これが超重要！）
        yield return null;

        // --- BGMの音量を復元 ---
        float savedBGM = PlayerPrefs.GetFloat(BGM_KEY, 1.0f);
        float clampedBGM = Mathf.Clamp(savedBGM, MIN_VOLUME, 1.0f);
        audioMixer.SetFloat(BGM_KEY, Mathf.Log10(clampedBGM) * 20);

        // --- SEの音量を復元 ---
        float savedSE = PlayerPrefs.GetFloat(SE_KEY, 1.0f);
        float clampedSE = Mathf.Clamp(savedSE, MIN_VOLUME, 1.0f);
        audioMixer.SetFloat(SE_KEY, Mathf.Log10(clampedSE) * 20);

        Debug.Log("別シーンでの音量復元が完了しました！");
    }
}
