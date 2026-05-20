using System;
using System.Collections.Generic;

[Serializable]
public class DroppedGearZone
{
    public string zoneId;
    public string fallenHeroName;
    public int floorIndex;
    public List<string> gearIds = new List<string>();
    public bool instantRetrieved = false;
}
