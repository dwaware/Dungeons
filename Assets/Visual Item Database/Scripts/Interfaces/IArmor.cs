using ItemSystem;

public interface IArmor : IBreakable
{
    ArmorType ArmorType { get; set; }
    int Defence { get; set; }
}