namespace GeoTutor.Core.Models;

public class Item
{
    public string Id { get; set; } = "";
    public string Template { get; set; } = "";
    public string ParamsJson { get; set; } = "{}";
    public string Prompt { get; set; } = "";
    public string AnswerJson { get; set; } = "{}";  // {value, tolerance} or {choices, correct}
    public List<string> Hints { get; set; } = [];
    public List<string> SolutionSteps { get; set; } = [];
    public string SkillId { get; set; } = "";
    public int Difficulty { get; set; } = 1;
    public string Source { get; set; } = "library"; // 'library' or 'llm'
    public ItemType Type { get; set; } = ItemType.Procedural;
}

public enum ItemType { Procedural, Conceptual, Transfer }
