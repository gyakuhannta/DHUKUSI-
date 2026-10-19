using System;
using UnityEngine;

public class geme_playerbase : MonoBehaviour
{


    [SerializeField]
    public const int maxcost=30;//最大コスト
    [SerializeField] 
    public const int mincost=0;//最小コスト
    public int cost=30;//現在コスト

    



    [SerializeField]
    public int ripacost=10;//回復コスト

    public int cost_no=1;//通常デュクシ
    public int cost_fe=3;//フェイント
    public int cost_co=5;//連続
    public int cost_he=7;//激重

    

    public void costbese()//コストのシステム試作
    {
        cost = Mathf.Clamp(cost, maxcost, mincost);

    }

    
    public void nomal() //通常デュクシ
    {
        cost-=cost_no;

    }
    public void feint() //フェイント
    {
        cost -= cost_fe;
    }
    public void combo() //連続
    {
        cost -= cost_co;
    }
    public void heavy() //激重
    {
        cost -= cost_he;
    }
    public void recovery()//コストゼロになった時の処理
    {
        if (cost <= 0)
        {
            cost += ripacost;
        }
    }

    


}
