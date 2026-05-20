using System;

[Serializable]
public class TraitData
{
    public string traitId;
    public string traitName;
    public string description;
    public bool isTrauma;

    // Trauma specific fields
    public string statCapField; // e.g. "maxAGI", "maxHP", etc.
    public float capPercent = 1.0f;    // e.g. 0.85f
}

[Serializable]
public class TraumaData : TraitData
{
    public TraumaData()
    {
        isTrauma = true;
    }
}
