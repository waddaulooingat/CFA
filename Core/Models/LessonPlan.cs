namespace GeoTutor.Core.Models;

public class LessonPlan
{
    public string SkillId { get; set; } = "";
    public int DifficultyBand { get; set; }
    public List<BeatDescriptor> Beats { get; set; } = [];
}

public class BeatDescriptor
{
    public BeatType Type { get; set; }
    public string TemplateRef { get; set; } = "";  // template name if applicable
    public string ParamsJson { get; set; } = "{}";
    public string Prose { get; set; } = "";
    public int ItemCount { get; set; } = 1;
}
