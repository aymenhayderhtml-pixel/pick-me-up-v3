using UnityEngine;

/// <summary>
/// ScriptableObject template for an enemy type.
/// One asset per enemy (e.g. "SO_Enemy_Goblin").
/// This is READ-ONLY data.
/// </summary>
[CreateAssetMenu(fileName = "SO_Enemy_New", menuName = "PickMeUp/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int    hp;
    public int    atk;
    public int    def;
    public int    spd;
    public float  critChance = 0.05f;
    public float  critMult   = 1.5f;
    public Sprite portrait;
    public bool   isBoss;
}
