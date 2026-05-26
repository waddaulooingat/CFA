namespace GeoTutor.Core.Models;

public class DialogueScript
{
    public string ErrorCategory { get; set; } = "";
    public List<DialogueLine> Lines { get; set; } = [];
    public string RetryProblemId { get; set; } = "";
}

public enum Speaker { Coach, Peer }

public class CanvasAction
{
    public string Type { get; set; } = ""; // highlight, morph, annotate, loadScene
    public string? Target { get; set; }
    public string? To { get; set; }
    public string? Label { get; set; }
}

public class DialogueLine
{
    public Speaker Speaker { get; set; }
    public string Text { get; set; } = "";
    public CanvasAction? Action { get; set; }
    public string? AudioFilePath { get; set; } // populated after TTS
}
