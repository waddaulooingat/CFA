namespace GeoTutor.Core.Models;

public class Lesson
{
    public string Id { get; set; } = "";
    public string SkillId { get; set; } = "";
    public List<Beat> Beats { get; set; } = [];
    public int Version { get; set; } = 1;
}

public enum BeatType { Concept, Manipulate, WorkedExample, Check, Practice, Reflection }

public class Beat
{
    public BeatType Type { get; set; }
    public string SceneSpecId { get; set; } = "";
    public string Prose { get; set; } = "";
    public List<string> ItemIds { get; set; } = [];
}
