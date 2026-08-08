namespace MenuBoard.Models;

public class DisplaySettings
{
    public int Id { get; set; }
    public int ScreenNumber { get; set; }
    public string BackgroundColor { get; set; } = "#000000";
    public string HeaderText { get; set; } = string.Empty;
    public double FontScale { get; set; } = 1.0;
}
