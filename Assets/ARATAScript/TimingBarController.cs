using UnityEngine;

public class TimingBarController : MonoBehaviour
{
    [SerializeField] private GameObject timingBar;
    // ©•ª‚ª–hŒä‚·‚é‚Æ‚«‚ÉŒÄ‚Ño‚·
    public void ShowTimingBar()
    {
        timingBar.SetActive(true);
    }
    // –hŒä‚ªI‚í‚Á‚½‚Æ‚«‚ÉŒÄ‚Ño‚·
    public void HideTimingBar()
    {
        timingBar.SetActive(false);
    }
}
