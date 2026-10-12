using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public bool fez1 = false;
    public bool fez2 = false;
     public bool fez3 = false;
     bool fez4 = false;

    private float taima =0;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fez1 = true;

        taima = 0;
    }

    // Update is called once per frame
    void Update()
    {

        var current = Keyboard.current;
        var aKey = current.aKey;

        taima += Time.deltaTime;

        

        if (fez1==true) {
            if (taima >= 3)
            {
                fez1 = false;
                fez2 = true;
                Debug.Log("フェーズ１終了");

            }
        }
        if (fez2==true )
        {
            if (taima >= 5)
            {
                fez2 = false;
                fez3 = true;
                Debug.Log("フェーズ2終了");
            }

        }
        if (fez3 == true)
        {
            if (taima >= 10 && aKey.wasPressedThisFrame)
            {
                Debug.Log("ゲームクリア");
                fez3=false;
            }
            else if(taima >=15) {

                fez3 = false;
                fez1=true;
                Debug.Log("ゲームオーバー");

            }

             


        }


    }
}
