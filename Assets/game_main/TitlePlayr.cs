using UnityEngine;

public class TitlePlayr : MonoBehaviour
{
    [SerializeField]
    Vector3[] weipos;
    [SerializeField]
    float movesp= 5f;
    public bool culafl = false;
    int pint = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void taitleMove() 
    {
        if(weipos == null||weipos.Length ==0)return;
        
        Vector3 taget= weipos[pint];
    }
}
