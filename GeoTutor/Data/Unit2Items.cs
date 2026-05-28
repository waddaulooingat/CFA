namespace GeoTutor.Data;

using GeoTutor.Core.Models;

/// <summary>
/// Library items for Unit 2 — Parallel Lines &amp; Transversals.
/// 3 items per skill (difficulties 1, 2, 3) × 5 skills = 15 items.
/// Used by LessonEngineService.BuildFallbackBeats when the LLM is offline.
///
/// Skills covered:
///   geo-u2-parallel-identify   — Identifying parallel lines
///   geo-u2-transversal-angles  — Corresponding, alternate interior/exterior, co-interior
///   geo-u2-parallel-proofs     — Proving lines parallel
///   geo-u2-perpendicular       — Perpendicular lines and distance
///   geo-u2-angle-relationships — Angle pair relationships in parallel line setups
/// </summary>
public static class Unit2Items
{
    public static readonly IReadOnlyList<Item> All = BuildAll();

    private static List<Item> BuildAll()
    {
        var items = new List<Item>();

        // ── geo-u2-parallel-identify : Identifying parallel lines ──────────

        items.Add(new Item
        {
            Id         = "u2-pid-001",
            SkillId    = "geo-u2-parallel-identify",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two lines in the same plane that never intersect are called ___ lines.",
            AnswerJson = """{"choices":["Parallel","Perpendicular","Skew","Transversal"],"correct":0}""",
            Hints      = [
                "Perpendicular lines DO intersect — at a 90° angle.",
                "Skew lines do not intersect but are in DIFFERENT planes. Parallel lines are in the SAME plane."
            ],
            SolutionSteps = [
                "Parallel lines lie in the same plane and never meet.",
                "Perpendicular lines intersect at 90°.",
                "Skew lines are non-coplanar (different planes).",
                "Answer: Parallel."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u2-pid-002",
            SkillId    = "geo-u2-parallel-identify",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Lines m and n are cut by a transversal. Two co-interior (same-side interior) angles measure 65° and 115°. Are lines m and n parallel?",
            AnswerJson = """{"choices":["Yes — co-interior angles sum to 180°, so lines are parallel","No — co-interior angles must be equal for lines to be parallel","Cannot determine from this information","No — both angles must be 90°"],"correct":0}""",
            Hints      = [
                "Co-interior angles (same-side interior) are supplementary (add to 180°) when lines are parallel.",
                "Check: 65° + 115° = 180°. Does that match the condition for parallel lines?"
            ],
            SolutionSteps = [
                "The Co-interior Angles Converse states: if co-interior angles sum to 180°, the lines are parallel.",
                "65° + 115° = 180° ✓",
                "Answer: Yes — the lines are parallel."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u2-pid-003",
            SkillId    = "geo-u2-parallel-identify",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Line AB passes through A(1, 2) and B(3, 4). Line CD passes through C(0, 1) and D(4, 5). Are lines AB and CD parallel?",
            AnswerJson = """{"choices":["Yes — both lines have slope 1","No — they have different slopes","Yes — they never intersect","No — they are the same line"],"correct":0}""",
            Hints      = [
                "Find the slope of each line using m = (y₂ − y₁)/(x₂ − x₁).",
                "Slope of AB = (4−2)/(3−1). Slope of CD = (5−1)/(4−0). Compare."
            ],
            SolutionSteps = [
                "Slope of AB = (4 − 2)/(3 − 1) = 2/2 = 1.",
                "Slope of CD = (5 − 1)/(4 − 0) = 4/4 = 1.",
                "Equal slopes with different y-intercepts → parallel lines.",
                "Answer: Yes — both lines have slope 1."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Transfer
        });

        // ── geo-u2-transversal-angles : Angle pair types ───────────────────

        items.Add(new Item
        {
            Id         = "u2-tva-001",
            SkillId    = "geo-u2-transversal-angles",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines are cut by a transversal. Angles that are between the parallel lines and on opposite sides of the transversal are called ___ angles.",
            AnswerJson = """{"choices":["Alternate interior","Alternate exterior","Co-interior","Corresponding"],"correct":0}""",
            Hints      = [
                "'Interior' means between the parallel lines. 'Alternate' means on opposite sides of the transversal.",
                "If the angles were on the SAME side, they would be co-interior (same-side interior) angles."
            ],
            SolutionSteps = [
                "Interior = between the two parallel lines.",
                "Alternate = on opposite sides of the transversal.",
                "Angles that are interior AND alternate → Alternate Interior angles.",
                "Answer: Alternate interior."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u2-tva-002",
            SkillId    = "geo-u2-transversal-angles",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines are cut by a transversal. One alternate interior angle measures 68°. What is the measure of the other alternate interior angle in degrees?",
            AnswerJson = """{"value":68,"tolerance":0.5}""",
            Hints      = [
                "When two parallel lines are cut by a transversal, alternate interior angles are congruent.",
                "Congruent means they have the same measure."
            ],
            SolutionSteps = [
                "Alternate Interior Angles Theorem: when parallel lines are cut by a transversal, alternate interior angles are equal.",
                "The other alternate interior angle = 68°.",
                "Answer: 68°."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u2-tva-003",
            SkillId    = "geo-u2-transversal-angles",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines are cut by a transversal. A pair of co-interior (same-side interior) angles measure (3x + 10)° and (2x + 20)°. What is the value of x?",
            AnswerJson = """{"value":30,"tolerance":0.01}""",
            Hints      = [
                "Co-interior angles are supplementary when lines are parallel — they add up to 180°.",
                "Set up: (3x + 10) + (2x + 20) = 180. Combine like terms, then solve for x."
            ],
            SolutionSteps = [
                "Co-interior angles add to 180°: (3x + 10) + (2x + 20) = 180",
                "5x + 30 = 180",
                "5x = 150",
                "x = 30"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u2-parallel-proofs : Proving lines parallel ────────────────

        items.Add(new Item
        {
            Id         = "u2-pp-001",
            SkillId    = "geo-u2-parallel-proofs",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "To prove two lines are parallel, you could show that alternate interior angles formed by a transversal are ___.",
            AnswerJson = """{"choices":["Congruent (equal)","Supplementary (sum to 180°)","Complementary (sum to 90°)","Vertical (opposite each other)"],"correct":0}""",
            Hints      = [
                "The Converse of the AIA Theorem: if alternate interior angles are equal, the lines are parallel.",
                "Co-interior angles being supplementary is a DIFFERENT converse theorem."
            ],
            SolutionSteps = [
                "The Converse of the Alternate Interior Angles Theorem states:",
                "If two lines are cut by a transversal so that alternate interior angles are congruent, then the lines are parallel.",
                "Answer: Congruent (equal)."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u2-pp-002",
            SkillId    = "geo-u2-parallel-proofs",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "∠3 and ∠6 are alternate interior angles formed by lines m and n cut by transversal t. If m∠3 = m∠6 = 72°, which theorem PROVES m ∥ n?",
            AnswerJson = """{"choices":["Converse of the Alternate Interior Angles Theorem","Alternate Interior Angles Theorem","Corresponding Angles Postulate","AA Similarity Theorem"],"correct":0}""",
            Hints      = [
                "We know the angles are equal and want to prove the lines are parallel — that's the CONVERSE.",
                "The AIA Theorem (not converse) is used when you already know lines are parallel."
            ],
            SolutionSteps = [
                "Given: m∠3 = m∠6 (alternate interior angles are congruent).",
                "The Converse of the AIA Theorem states: if alternate interior angles are congruent → lines are parallel.",
                "Therefore m ∥ n by the Converse of the Alternate Interior Angles Theorem."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u2-pp-003",
            SkillId    = "geo-u2-parallel-proofs",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two lines are cut by a transversal. Corresponding angles measure (5x − 20)° and (3x + 40)°. For what value of x are the lines parallel?",
            AnswerJson = """{"value":30,"tolerance":0.01}""",
            Hints      = [
                "When lines are parallel, corresponding angles are equal (Converse of Corresponding Angles Postulate).",
                "Set the expressions equal: 5x − 20 = 3x + 40. Solve for x."
            ],
            SolutionSteps = [
                "Corresponding angles are equal when lines are parallel:",
                "5x − 20 = 3x + 40",
                "2x = 60",
                "x = 30",
                "Check: 5(30) − 20 = 130°, 3(30) + 40 = 130° ✓"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u2-perpendicular : Perpendicular lines and distance ────────

        items.Add(new Item
        {
            Id         = "u2-perp-001",
            SkillId    = "geo-u2-perpendicular",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two lines that intersect to form a right angle are called ___ lines.",
            AnswerJson = """{"choices":["Perpendicular","Parallel","Skew","Transversal"],"correct":0}""",
            Hints      = [
                "Parallel lines never intersect. These lines DO intersect.",
                "The intersection forms a 90° angle — the defining property of perpendicular lines."
            ],
            SolutionSteps = [
                "Perpendicular lines intersect at exactly 90° (a right angle).",
                "The symbol ⊥ is used to denote perpendicularity.",
                "Answer: Perpendicular."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u2-perp-002",
            SkillId    = "geo-u2-perpendicular",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Line k has slope 4. What is the slope of a line perpendicular to line k? Enter your answer as a decimal.",
            AnswerJson = """{"value":-0.25,"tolerance":0.01}""",
            Hints      = [
                "Perpendicular lines have slopes that are negative reciprocals of each other.",
                "Negative reciprocal of 4 = −1/4. Convert to decimal."
            ],
            SolutionSteps = [
                "If slope of k is m, slope of a perpendicular line is −1/m.",
                "−1/4 = −0.25.",
                "Answer: −0.25."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u2-perp-003",
            SkillId    = "geo-u2-perpendicular",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Segment AB has endpoints A(1, 1) and B(7, 9). What is the slope of the perpendicular bisector of AB? Enter as a decimal.",
            AnswerJson = """{"value":-0.75,"tolerance":0.01}""",
            Hints      = [
                "First find the slope of AB: m = (9−1)/(7−1).",
                "The perpendicular bisector has slope = −1/(slope of AB)."
            ],
            SolutionSteps = [
                "Slope of AB = (9 − 1)/(7 − 1) = 8/6 = 4/3.",
                "Perpendicular slope = −1/(4/3) = −3/4 = −0.75.",
                "Answer: −0.75."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Transfer
        });

        // ── geo-u2-angle-relationships : Angle pairs in parallel setups ─────

        items.Add(new Item
        {
            Id         = "u2-ar-001",
            SkillId    = "geo-u2-angle-relationships",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "When two parallel lines are cut by a transversal, co-interior (same-side interior) angles are ___.",
            AnswerJson = """{"choices":["Supplementary (sum to 180°)","Congruent (equal)","Complementary (sum to 90°)","Vertical (always equal)"],"correct":0}""",
            Hints      = [
                "Alternate interior angles are EQUAL (congruent) when lines are parallel.",
                "Co-interior (same-side interior) angles behave differently — what do they add up to?"
            ],
            SolutionSteps = [
                "Co-Interior Angles Theorem: when parallel lines are cut by a transversal, co-interior angles add to 180°.",
                "They are supplementary, not congruent.",
                "Answer: Supplementary (sum to 180°)."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u2-ar-002",
            SkillId    = "geo-u2-angle-relationships",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines are cut by a transversal. A pair of corresponding angles measure (4x + 15)° and 75°. Find the value of x.",
            AnswerJson = """{"value":15,"tolerance":0.01}""",
            Hints      = [
                "When lines are parallel, corresponding angles are congruent (equal).",
                "Set 4x + 15 = 75 and solve for x."
            ],
            SolutionSteps = [
                "Corresponding angles are equal when lines are parallel:",
                "4x + 15 = 75",
                "4x = 60",
                "x = 15"
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u2-ar-003",
            SkillId    = "geo-u2-angle-relationships",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two parallel lines p and q are cut by transversal t. Alternate exterior angles are labeled (6x − 12)° and (4x + 28)°. What is the value of x?",
            AnswerJson = """{"value":20,"tolerance":0.01}""",
            Hints      = [
                "Alternate exterior angles are congruent when lines are parallel.",
                "Set 6x − 12 = 4x + 28 and solve."
            ],
            SolutionSteps = [
                "Alternate Exterior Angles Theorem: when parallel lines are cut by a transversal, alternate exterior angles are equal.",
                "6x − 12 = 4x + 28",
                "2x = 40",
                "x = 20",
                "Check: 6(20) − 12 = 108°, 4(20) + 28 = 108° ✓"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        return items;
    }
}
