namespace GeoTutor.Core.Models;

public class LlmEnvelope
{
    public string Template { get; set; } = "";
    public Dictionary<string, object> Params { get; set; } = [];
    public string Prompt { get; set; } = "";
    public LlmAnswer Answer { get; set; } = new();
    public List<string> SolutionSteps { get; set; } = [];
    public List<string> Hints { get; set; } = [];
    public string SkillTag { get; set; } = "";
    public int Difficulty { get; set; } = 1;
}

public class LlmAnswer
{
    public double? Value { get; set; }
    public double? Tolerance { get; set; }
    public List<string>? Choices { get; set; }
    public int? CorrectIndex { get; set; }
}
