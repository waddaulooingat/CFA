namespace GeoTutor.SceneEngine.Templates;
using GeoTutor.SceneEngine.Models;
using System.Collections.Generic;

public static class TemplateRegistry
{
    private static readonly Dictionary<string, ISceneTemplate> _templates;

    static TemplateRegistry()
    {
        _templates = new Dictionary<string, ISceneTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["GenericTriangle"]               = new GenericTriangleTemplate(),
            ["RightTriangle"]                 = new RightTriangleTemplate(),
            ["IsoscelesTriangle"]             = new IsoscelesTriangleTemplate(),
            ["EquilateralTriangle"]           = new EquilateralTriangleTemplate(),
            ["ParallelLinesTransversal"]      = new ParallelLinesTransversalTemplate(),
            ["TwoCongruentTriangles"]         = new TwoCongruentTrianglesTemplate(),
            ["SimilarTriangles"]              = new SimilarTrianglesTemplate(),
            ["Circle"]                        = new CircleTemplate(),
            ["CircleWithChord"]               = new CircleWithChordTemplate(),
            ["CircleWithInscribedAngle"]      = new CircleWithInscribedAngleTemplate(),
            ["CircleWithTangent"]             = new CircleWithTangentTemplate(),
            ["Parallelogram"]                 = new ParallelogramTemplate(),
            ["Trapezoid"]                     = new TrapezoidTemplate(),
            ["CoordinatePolygon"]             = new CoordinatePolygonTemplate(),
            ["TransformationPair"]            = new TransformationPairTemplate(),
        };
    }

    public static ISceneTemplate? Get(string name)
        => _templates.TryGetValue(name, out var t) ? t : null;

    public static bool TryBuild(
        string name,
        Dictionary<string, object> parameters,
        out SceneSpec? spec,
        out string error)
    {
        spec = null;
        if (!_templates.TryGetValue(name, out var template))
        {
            error = $"Unknown template '{name}'.";
            return false;
        }
        if (!template.ValidateParams(parameters, out error))
            return false;
        try
        {
            spec = template.Build(parameters);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Template '{name}' threw: {ex.Message}";
            return false;
        }
    }
}
