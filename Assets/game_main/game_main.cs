using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private bool fez1 = false;
    private bool fez2 = false;
     private bool fez3 = false;
     bool fez4 = false;

    private float taima;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fez1 = true;
    }

    // Update is called once per frame
    void Update()
    {

        var current = Keyboard.current;
        var aKey = current.aKey;

        

        if (fez1) { }


    }
}
