namespace WebLinhKien.Models.Entities;

public class CaiDat
{
    public int Uid { get; set; }
    public string Khoa { get; set; } = string.Empty;
    public string GiaTri { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}
