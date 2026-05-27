namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;

public interface ISceneTemplate
{
    string TemplateName { get; }
    SceneSpec Build(Dictionary<string, object> parameters);
    bool ValidateParams(Dictionary<string, object> parameters, out string error);
}
