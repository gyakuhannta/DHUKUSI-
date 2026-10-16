using UnityEngine;

public class geme_playerbase : MonoBehaviour
{


    [SerializeField]
    public int maxcost=30;//最大コスト
    [SerializeField]
    public int ripacost=10;//回復コスト

    public int cost_no=1;//通常デュクシ
    public int cost_fe=3;//フェイント
    public int cost_co=5;//連続
    public int cost_he=7;//激重

    


    
    public void nomal() //通常デュクシ
    {
        maxcost-=cost_no;

    }
    public void feint() //フェイント
    {
        maxcost -= cost_fe;
    }
    public void combo() //連続
    {
        maxcost -= cost_co;
    }
    public void heavy() //激重
    {
        maxcost -= cost_he;
    }
    public void recovery()
    {
        maxcost += ripacost;
    }

    


}
