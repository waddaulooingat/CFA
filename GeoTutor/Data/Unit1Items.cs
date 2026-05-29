namespace GeoTutor.Data;

using GeoTutor.Core.Models;

/// <summary>
/// Library items for Unit 1 — Foundations &amp; Logical Reasoning.
/// 3 items per skill (difficulties 1, 2, 3) × 5 skills = 15 items.
/// Used by LessonEngineService.BuildFallbackBeats when the LLM is offline.
///
/// Skills covered (new IDs from unit-01-skills.json):
///   geo-u1-point-line-plane  — Points, Lines, and Planes
///   geo-u1-angle-measure     — Angle Measure and Types
///   geo-u1-if-then-logic     — If-Then Logic (+ converse / contrapositive items)
///   geo-u1-counterexamples   — Counterexamples
/// </summary>
public static class Unit1Items
{
    public static readonly IReadOnlyList<Item> All = BuildAll();

    private static List<Item> BuildAll()
    {
        var items = new List<Item>();

        // ── geo-u1-definitions : Points, lines, planes ─────────────────────

        items.Add(new Item
        {
            Id         = "u1-def-001",
            SkillId    = "geo-u1-point-line-plane",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "A portion of a line that has exactly two endpoints is called a ___.",
            AnswerJson = """{"choices":["Line segment","Ray","Line","Plane"],"correct":0}""",
            Hints      = [
                "A line extends infinitely in both directions. A ray extends infinitely in only one direction.",
                "The key word is 'two endpoints' — this means the figure has a definite start and end."
            ],
            SolutionSteps = [
                "A line has no endpoints and extends infinitely.",
                "A ray has one endpoint and extends infinitely in one direction.",
                "A line segment has two endpoints — a definite start and end.",
                "Answer: Line segment."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-def-002",
            SkillId    = "geo-u1-point-line-plane",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Two distinct lines in the same plane that never intersect are called ___.",
            AnswerJson = """{"choices":["Parallel lines","Perpendicular lines","Skew lines","Intersecting lines"],"correct":0}""",
            Hints      = [
                "Perpendicular lines DO intersect — at a right angle.",
                "Skew lines are in different planes. These two lines are in the SAME plane."
            ],
            SolutionSteps = [
                "Two lines in the same plane either intersect or never meet.",
                "Lines that never meet in the same plane are called parallel lines.",
                "Answer: Parallel lines."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-def-003",
            SkillId    = "geo-u1-point-line-plane",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Point B lies between points A and C on a segment. AB = 2x + 3, BC = x + 7, and AC = 28. What is the value of x?",
            AnswerJson = """{"value":6,"tolerance":0.01}""",
            Hints      = [
                "The Segment Addition Postulate says: AB + BC = AC.",
                "Substitute: (2x + 3) + (x + 7) = 28. Combine like terms."
            ],
            SolutionSteps = [
                "By the Segment Addition Postulate: AB + BC = AC",
                "(2x + 3) + (x + 7) = 28",
                "3x + 10 = 28",
                "3x = 18",
                "x = 6"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u1-angles : Angle types ─────────────────────────────────────

        items.Add(new Item
        {
            Id         = "u1-ang-001",
            SkillId    = "geo-u1-angle-measure",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "An angle that measures less than 90° is called a ___ angle.",
            AnswerJson = """{"choices":["Acute","Obtuse","Right","Straight"],"correct":0}""",
            Hints      = [
                "A right angle is exactly 90°.",
                "An obtuse angle is between 90° and 180°. An acute angle is less than 90°."
            ],
            SolutionSteps = [
                "Acute: 0° < angle < 90°",
                "Right: exactly 90°",
                "Obtuse: 90° < angle < 180°",
                "Straight: exactly 180°",
                "Answer: Acute."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-ang-002",
            SkillId    = "geo-u1-angle-measure",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "∠ABC and ∠CBD are a linear pair. m∠CBD = x + 30° and m∠ABC = 3x + 10°. What is m∠ABC in degrees?",
            AnswerJson = """{"value":115,"tolerance":0.5}""",
            Hints      = [
                "A linear pair of angles are supplementary — they add up to 180°.",
                "Set up the equation: (3x + 10) + (x + 30) = 180, then solve for x."
            ],
            SolutionSteps = [
                "Linear pair → supplementary: m∠ABC + m∠CBD = 180°",
                "(3x + 10) + (x + 30) = 180",
                "4x + 40 = 180",
                "4x = 140",
                "x = 35",
                "m∠ABC = 3(35) + 10 = 105 + 10 = 115°"
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u1-ang-003",
            SkillId    = "geo-u1-angle-measure",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The measure of an angle's supplement is 30° more than twice the angle's complement. What is the measure of the angle in degrees?",
            AnswerJson = """{"value":30,"tolerance":0.5}""",
            Hints      = [
                "Let the angle be x°. Supplement = 180 − x. Complement = 90 − x.",
                "Write the equation: 180 − x = 2(90 − x) + 30. Then solve for x."
            ],
            SolutionSteps = [
                "Let x = the angle's measure.",
                "Supplement = 180 − x; Complement = 90 − x.",
                "180 − x = 2(90 − x) + 30",
                "180 − x = 180 − 2x + 30",
                "180 − x = 210 − 2x",
                "x = 30°"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Transfer
        });

        // ── geo-u1-logic-ifthen : If-then statements ────────────────────────

        items.Add(new Item
        {
            Id         = "u1-logic-001",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "In the conditional statement 'If today is Friday, then tomorrow is Saturday', what is the conclusion?",
            AnswerJson = """{"choices":["Tomorrow is Saturday","Today is Friday","Today is not Friday","Tomorrow is not Saturday"],"correct":0}""",
            Hints      = [
                "A conditional has the form 'If [hypothesis], then [conclusion]'.",
                "The hypothesis is the 'if' part; the conclusion is the 'then' part."
            ],
            SolutionSteps = [
                "Hypothesis (if-part): 'today is Friday'.",
                "Conclusion (then-part): 'tomorrow is Saturday'.",
                "Answer: Tomorrow is Saturday."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-logic-002",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The conditional 'If a shape is a square, then it has four equal sides' is true. A shape has four equal sides. Can we logically conclude it is a square?",
            AnswerJson = """{"choices":["No — this would be using the converse, which is not guaranteed to be true","Yes — it must be a square","Only if it also has four right angles","Not enough information to decide"],"correct":0}""",
            Hints      = [
                "We know p → q is true. We also know q is true. Can we conclude p?",
                "This pattern is called 'affirming the consequent' — it is a logical fallacy."
            ],
            SolutionSteps = [
                "The conditional p → q: 'square → four equal sides' is true.",
                "Knowing q ('four equal sides') does NOT let us conclude p ('it is a square').",
                "A rhombus also has four equal sides but is not always a square.",
                "Using the converse (q → p) without knowing it is true is a logical error.",
                "Answer: No — this is the converse fallacy."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-logic-003",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Which argument form is logically VALID? (p → q means 'if p then q')",
            AnswerJson = """{"choices":["If p→q and p is true, then q is true (Modus Ponens)","If p→q and q is true, then p is true","If p→q and p is false, then q is false","If p→q and q is false, then p is true"],"correct":0}""",
            Hints      = [
                "Modus Ponens: If p→q is true and p is true, then q must be true.",
                "The other choices are common logical fallacies (affirming the consequent, denying the antecedent)."
            ],
            SolutionSteps = [
                "Modus Ponens (valid): p→q, p ∴ q.",
                "Affirming the consequent (invalid): p→q, q ∴ p.",
                "Denying the antecedent (invalid): p→q, ¬p ∴ ¬q.",
                "Answer: Modus Ponens — if p→q and p, then q."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-u1-converse : Converse, inverse, contrapositive ────────────

        items.Add(new Item
        {
            Id         = "u1-conv-001",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "What is the converse of: 'If it is snowing, then school is cancelled'?",
            AnswerJson = """{"choices":["If school is cancelled, then it is snowing","If it is not snowing, then school is not cancelled","If school is not cancelled, then it is not snowing","It is snowing if and only if school is cancelled"],"correct":0}""",
            Hints      = [
                "The converse of 'If p, then q' is 'If q, then p'.",
                "Swap the hypothesis and conclusion."
            ],
            SolutionSteps = [
                "Original: If p (snowing), then q (school cancelled).",
                "Converse: If q (school cancelled), then p (snowing).",
                "Answer: 'If school is cancelled, then it is snowing.'"
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u1-conv-002",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "What is the contrapositive of: 'If ∠A is a right angle, then m∠A = 90°'?",
            AnswerJson = """{"choices":["If m∠A ≠ 90°, then ∠A is not a right angle","If ∠A is not a right angle, then m∠A ≠ 90°","If m∠A = 90°, then ∠A is a right angle","None of the above"],"correct":0}""",
            Hints      = [
                "The contrapositive of 'If p, then q' is 'If NOT q, then NOT p'.",
                "Negate both parts AND swap them."
            ],
            SolutionSteps = [
                "Original: If p (∠A is right), then q (m∠A = 90°).",
                "Contrapositive: If ¬q (m∠A ≠ 90°), then ¬p (∠A is not right).",
                "Answer: 'If m∠A ≠ 90°, then ∠A is not a right angle.'"
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u1-conv-003",
            SkillId    = "geo-u1-if-then-logic",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "A conditional statement is true. Which of the following must ALSO be true?",
            AnswerJson = """{"choices":["Its contrapositive","Its converse","Its inverse","All three"],"correct":0}""",
            Hints      = [
                "The contrapositive is logically equivalent to the original conditional.",
                "The converse and inverse are logically equivalent to EACH OTHER, but not necessarily to the original."
            ],
            SolutionSteps = [
                "If p→q is true, its contrapositive (¬q→¬p) is always also true.",
                "The converse (q→p) and inverse (¬p→¬q) may or may not be true.",
                "Answer: Its contrapositive."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-u1-counterexample : Counterexamples ─────────────────────────

        items.Add(new Item
        {
            Id         = "u1-cex-001",
            SkillId    = "geo-u1-counterexamples",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Which of the following is a counterexample to the statement 'All rectangles are squares'?",
            AnswerJson = """{"choices":["A rectangle with length 6 and width 4","A square with side length 5","A rhombus with four right angles","A quadrilateral with all right angles and equal sides"],"correct":0}""",
            Hints      = [
                "A counterexample is a specific case that makes the statement FALSE.",
                "A rectangle requires four right angles but NOT necessarily equal sides."
            ],
            SolutionSteps = [
                "Squares are rectangles, but not all rectangles are squares.",
                "A rectangle with length 6 and width 4 has right angles but unequal adjacent sides.",
                "This is NOT a square, so it disproves the statement.",
                "Answer: A rectangle with length 6 and width 4."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-cex-002",
            SkillId    = "geo-u1-counterexamples",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Which value of n is a counterexample to the statement 'For all integers n, n² > n'?",
            AnswerJson = """{"choices":["n = 0  (0² = 0, which is not > 0)","n = 2  (4 > 2)","n = 3  (9 > 3)","n = 5  (25 > 5)"],"correct":0}""",
            Hints      = [
                "Try small or special values of n — what happens at n = 0 or n = 1?",
                "0² = 0, which is NOT greater than 0."
            ],
            SolutionSteps = [
                "For n = 0: 0² = 0.  Is 0 > 0? No.",
                "This disproves the statement for all integers n.",
                "Answer: n = 0."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u1-cex-003",
            SkillId    = "geo-u1-counterexamples",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The statement 'If a quadrilateral has four equal angles, then it has four equal sides' is false. Which of the following is a counterexample?",
            AnswerJson = """{"choices":["A rectangle that is not a square","A rhombus","A parallelogram with no right angles","A trapezoid"],"correct":0}""",
            Hints      = [
                "A quadrilateral with four equal angles has all angles = 90° (right angles). What shape is that?",
                "A rectangle has four right angles (equal angles) but its sides are not necessarily all equal."
            ],
            SolutionSteps = [
                "Four equal angles in a quadrilateral → each angle = 360°/4 = 90°.",
                "A rectangle has four 90° angles but adjacent sides need not be equal.",
                "So a non-square rectangle disproves the statement.",
                "Answer: A rectangle that is not a square."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Transfer
        });

        return items;
    }
}
