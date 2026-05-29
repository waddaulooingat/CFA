namespace GeoTutor.Data;

using GeoTutor.Core.Models;

/// <summary>
/// Hard-coded geometry prerequisite items for the baseline assessment (§3.2).
/// 15 items across 6 foundational topics.  Three topics map to existing unit-1/2
/// skill IDs; three require new unit-0 skill nodes seeded via App.xaml.cs.
///
/// Skill ID mapping:
///   geo-u1-angle-measure        → angle vocabulary (comp, supp, vertical)
///   geo-u2-transversal-angles   → parallel lines / transversal angle pairs
///   geo-u1-if-then-logic        → if-then statements, converse, counterexample
///   geo-prereq-triangle-basics  → triangle angle sum and classification
///   geo-prereq-coord-plane      → coordinate plane fluency
///   geo-prereq-area-perimeter   → area, perimeter, and circumference basics
/// </summary>
public static class GeometryPrereqItems
{
    public static readonly IReadOnlyList<Item> All = BuildAll();

    private static List<Item> BuildAll()
    {
        var items = new List<Item>();

        // ── geo-u1-angles : Angle vocabulary ─────────────────────────────
        items.Add(new Item
        {
            Id         = "geo-prereq-angles-001",
            SkillId    = "geo-u1-angle-measure",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two angles are complementary (they add up to 90°). One angle measures 35°. What is the measure of the other angle, in degrees?",
            AnswerJson = """{"value":55,"tolerance":0.5}""",
            Hints      = ["Complementary angles sum to 90°.", "90 − 35 = ?"],
            SolutionSteps = ["Complementary angles add to 90°.", "90 − 35 = 55°"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-angles-002",
            SkillId    = "geo-u1-angle-measure",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two angles are supplementary (they add up to 180°). One angle measures 112°. What is the measure of the other angle, in degrees?",
            AnswerJson = """{"value":68,"tolerance":0.5}""",
            Hints      = ["Supplementary angles sum to 180°.", "180 − 112 = ?"],
            SolutionSteps = ["180 − 112 = 68°"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-angles-003",
            SkillId    = "geo-u1-angle-measure",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two straight lines intersect, forming four angles. One angle measures 74°. Which of the following is the measure of the vertical angle (the angle directly opposite)?",
            AnswerJson = """{"choices":["74°","106°","16°","180°"],"correct":0}""",
            Hints      = ["Vertical angles are formed by two intersecting lines and are always equal.", "The angle directly across the intersection point from 74° is also 74°."],
            SolutionSteps = ["Vertical angles are congruent.", "The vertical angle also measures 74°."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-u2-transversal-angles : Parallel lines / transversal ─────
        items.Add(new Item
        {
            Id         = "geo-prereq-transversal-001",
            SkillId    = "geo-u2-transversal-angles",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines are cut by a transversal. One of the angles formed measures 65°. What is the measure of the corresponding angle?",
            AnswerJson = """{"value":65,"tolerance":0.5}""",
            Hints      = ["Corresponding angles are on the same side of the transversal, both above (or both below) a parallel line.", "When lines are parallel, corresponding angles are equal."],
            SolutionSteps = ["Corresponding angles are congruent when lines are parallel.", "The corresponding angle = 65°."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-transversal-002",
            SkillId    = "geo-u2-transversal-angles",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines are cut by a transversal. One co-interior (same-side interior) angle measures 70°. What is the measure of the other co-interior angle?",
            AnswerJson = """{"value":110,"tolerance":0.5}""",
            Hints      = ["Co-interior angles (also called consecutive interior or same-side interior) are supplementary — they add up to 180°.", "180 − 70 = ?"],
            SolutionSteps = ["Co-interior angles are supplementary.", "180 − 70 = 110°"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u1-logic-ifthen : Logical reasoning ──────────────────────
        items.Add(new Item
        {
            Id         = "geo-prereq-logic-001",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Consider the statement: \"If a figure is a square, then it has four right angles.\"  What is the hypothesis of this conditional statement?",
            AnswerJson = """{"choices":["A figure is a square","It has four right angles","A figure has four sides","It is a quadrilateral"],"correct":0}""",
            Hints      = ["The hypothesis is the 'if' part of the if-then statement.", "It is the condition that is assumed to be true."],
            SolutionSteps = ["Hypothesis (if-part): 'a figure is a square'.", "Conclusion (then-part): 'it has four right angles'."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-logic-002",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Which of the following is a counterexample to the statement: \"All quadrilaterals are rectangles\"?",
            AnswerJson = """{"choices":["A parallelogram that is not a rectangle","A square","A rectangle with sides 3 and 5","A rectangle with all equal sides"],"correct":0}""",
            Hints      = ["A counterexample is a specific case that shows the statement is false.", "Find a quadrilateral that is NOT a rectangle."],
            SolutionSteps = ["A parallelogram can have four sides (quadrilateral) but not necessarily four right angles.", "So a non-rectangular parallelogram is a counterexample."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-prereq-triangle-basics : Triangle angle sum & classification
        items.Add(new Item
        {
            Id         = "geo-prereq-triangle-001",
            SkillId    = "geo-prereq-triangle-basics",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The three interior angles of a triangle are 45°, 60°, and x°. What is the value of x?",
            AnswerJson = """{"value":75,"tolerance":0.5}""",
            Hints      = ["The angles of any triangle add up to 180°.", "45 + 60 + x = 180."],
            SolutionSteps = ["45 + 60 + x = 180", "105 + x = 180", "x = 75"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-triangle-002",
            SkillId    = "geo-prereq-triangle-basics",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "A triangle has two angles measuring 40° and 65°. What is the measure of the exterior angle at the third vertex?",
            AnswerJson = """{"value":105,"tolerance":0.5}""",
            Hints      = ["The exterior angle of a triangle equals the sum of the two non-adjacent interior angles.", "Exterior angle = 40 + 65."],
            SolutionSteps = ["Third interior angle = 180 − 40 − 65 = 75°.", "Exterior angle = 180 − 75 = 105°.", "Or directly: exterior angle = 40 + 65 = 105°."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-triangle-003",
            SkillId    = "geo-prereq-triangle-basics",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "A triangle has side lengths 5, 5, and 8. How is this triangle best classified?",
            AnswerJson = """{"choices":["Isosceles","Equilateral","Scalene","Right"],"correct":0}""",
            Hints      = ["Equilateral: all three sides equal.", "Isosceles: exactly two sides equal.", "Scalene: all sides different."],
            SolutionSteps = ["Two sides equal (5 = 5), one different (8).", "Therefore: isosceles triangle."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-prereq-coord-plane : Coordinate plane fluency ─────────────
        items.Add(new Item
        {
            Id         = "geo-prereq-coord-001",
            SkillId    = "geo-prereq-coord-plane",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Point P is located at (−4, 3) in the coordinate plane. In which quadrant is point P?",
            AnswerJson = """{"choices":["Quadrant II","Quadrant I","Quadrant III","Quadrant IV"],"correct":0}""",
            Hints      = ["Quadrant I: (+, +)  II: (−, +)  III: (−, −)  IV: (+, −).", "x is negative, y is positive."],
            SolutionSteps = ["x = −4 (negative), y = 3 (positive) → Quadrant II."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-coord-002",
            SkillId    = "geo-prereq-coord-plane",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Point A is at (5, −2). If it is reflected across the y-axis, what are the new coordinates?",
            AnswerJson = """{"choices":["(−5, −2)","(5, 2)","(−5, 2)","(2, −5)"],"correct":0}""",
            Hints      = ["Reflecting across the y-axis negates the x-coordinate.", "The y-coordinate stays the same."],
            SolutionSteps = ["Reflection across y-axis: (x, y) → (−x, y).", "(5, −2) → (−5, −2)."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-prereq-area-perimeter : Area, perimeter, circumference ────
        items.Add(new Item
        {
            Id         = "geo-prereq-area-001",
            SkillId    = "geo-prereq-area-perimeter",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "A rectangle has a length of 9 and a width of 4. What is its area?",
            AnswerJson = """{"value":36,"tolerance":0.5}""",
            Hints      = ["Area of a rectangle = length × width."],
            SolutionSteps = ["Area = 9 × 4 = 36"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-area-002",
            SkillId    = "geo-prereq-area-perimeter",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "A triangle has a base of 10 and a height of 6. What is its area?",
            AnswerJson = """{"value":30,"tolerance":0.5}""",
            Hints      = ["Area of a triangle = ½ × base × height."],
            SolutionSteps = ["Area = ½ × 10 × 6 = 30"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "geo-prereq-area-003",
            SkillId    = "geo-prereq-area-perimeter",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Which formula gives the circumference of a circle with radius r?",
            AnswerJson = """{"choices":["C = 2πr","C = πr²","C = πr","C = 4πr"],"correct":0}""",
            Hints      = ["The circumference formula uses the diameter d = 2r.", "C = π × diameter = π × 2r."],
            SolutionSteps = ["Circumference C = 2πr."],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        return items;
    }
}
