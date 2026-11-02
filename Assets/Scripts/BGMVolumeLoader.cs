using UnityEngine;
using UnityEngine.Audio;

public class BGMVolumeLoader : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer; 
    private string parameterName = "BGMVolume";    

    void Start()
    {
        
        float savedVolume = PlayerPrefs.GetFloat("BGM_Volume_Key", 1.0f);

       
        float dB = Mathf.Log10(Mathf.Max(savedVolume, 0.0001f)) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }
}

