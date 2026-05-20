using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject template for a Tower Floor configuration.
/// Place in Assets/Resources/Floors/ as SO assets (e.g. "SO_Floor_1").
/// </summary>
[CreateAssetMenu(fileName = "SO_Floor_New", menuName = "PickMeUp/Tower Floor")]
public class TowerFloor : ScriptableObject
{
    [Header("Identity")]
    public int floorNumber;
    public string floorTitle = "Gravely Hall";

    [Header("Requirements & Rewards")]
    [Tooltip("Stamina required to enter this floor")]
    public int staminaCost = 5;

    [Tooltip("Gold earned on successful floor clear")]
    public int goldReward = 150;

    [Tooltip("XP earned by survivors on successful clear")]
    public int xpReward = 50;

    [Header("Combat Pool")]
    [Tooltip("List of possible enemies that spawn on this floor")]
    public List<EnemyData> enemyPool = new List<EnemyData>();
}
