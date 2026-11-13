using System.Reflection;
using UnityEngine;

/// <summary>
/// 相手キャラ（ステージのまん中の人）のアニメーション
///  ・ふだんは待機（taki）
///  ・相手の攻撃が始まったら、その技のアニメーション（通常 / フェイント / 連続 / 激重）をして、待機にもどる
///  ・攻撃の判定のスクリプト（TimingJudge）は変えずに、「防御中か」と「相手の技」を読むだけ
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyCharacterAnim : MonoBehaviour
{
    // Animator のトリガー名（TimingJudge の correctZone の 0〜3 と同じ順番）
    public static readonly string[] Triggers = { "Normal", "Feint", "Renzoku", "Gekiomo" };

    private Animator animator;
    private TimingJudge judge;
    private FieldInfo defendingField, zoneField;
    private bool wasDefending;

    void Start()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;   // アニメーションで場所がずれないようにする
        judge = FindFirstObjectByType<TimingJudge>();
        const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        defendingField = typeof(TimingJudge).GetField("isDefending", f);
        zoneField = typeof(TimingJudge).GetField("correctZone", f);
    }

    void Update()
    {
        if (judge == null || defendingField == null) return;
        bool defending = (bool)defendingField.GetValue(judge);
        if (defending && !wasDefending)
        {
            // AlphaBattleController が相手の技を払える技に変えることがあるので、1フレーム待ってから読む
            Invoke(nameof(PlayAttack), 0.05f);
        }
        wasDefending = defending;
    }

    private void PlayAttack()
    {
        if (!(bool)defendingField.GetValue(judge)) return;   // コスト切れで休んだときは何もしない
        int zone = zoneField != null ? (int)zoneField.GetValue(judge) : 0;
        if (zone < 0 || zone >= Triggers.Length) zone = 0;
        animator.SetTrigger(Triggers[zone]);
    }
}
