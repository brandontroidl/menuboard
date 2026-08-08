namespace MenuBoard.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int ScreenNumber { get; set; }
    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
