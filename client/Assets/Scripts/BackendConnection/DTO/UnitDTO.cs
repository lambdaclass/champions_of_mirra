using System;

[Serializable]
public class UnitDTO
{
    public string id { get; set; }
    public int? slot { get; set; }
    public int level { get; set; }
    public bool selected { get; set; }
    public string character { get; set; }
}
