namespace GeoTutor.Data;

using GeoTutor.Core.Models;

/// <summary>
/// Hard-coded algebra prerequisite items for the baseline assessment.
/// 3 items per skill (difficulty 1, 2, 2) × 8 skills = 24 total.
/// Source = "library" so BaselineViewModel's FetchItemForSkill query finds them.
/// </summary>
public static class AlgebraItems
{
    public static readonly IReadOnlyList<Item> All = BuildAll();

    private static List<Item> BuildAll()
    {
        var items = new List<Item>();

        // ── alg-linear-eq : Solving linear equations ──────────────────────
        items.Add(new Item
        {
            Id         = "alg-linear-eq-001",
            SkillId    = "alg-linear-eq",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Solve for x:  3x + 7 = 22",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["Subtract 7 from both sides first.", "After subtracting: 3x = 15.  Now divide both sides by 3."],
            SolutionSteps = ["3x + 7 = 22", "3x = 15", "x = 5"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-linear-eq-002",
            SkillId    = "alg-linear-eq",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Solve for x:  2(x − 4) = 10",
            AnswerJson = """{"value":9,"tolerance":0.01}""",
            Hints      = ["Distribute: 2x − 8 = 10.", "Add 8 to both sides, then divide by 2."],
            SolutionSteps = ["2(x − 4) = 10", "2x − 8 = 10", "2x = 18", "x = 9"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-linear-eq-003",
            SkillId    = "alg-linear-eq",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Solve for x:  (x / 3) − 2 = 4",
            AnswerJson = """{"value":18,"tolerance":0.01}""",
            Hints      = ["Add 2 to both sides: x/3 = 6.", "Multiply both sides by 3."],
            SolutionSteps = ["x/3 − 2 = 4", "x/3 = 6", "x = 18"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── alg-systems : Systems of equations ────────────────────────────
        items.Add(new Item
        {
            Id         = "alg-systems-001",
            SkillId    = "alg-systems",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Solve by substitution.  y = 2x and x + y = 12.  What is x?",
            AnswerJson = """{"value":4,"tolerance":0.01}""",
            Hints      = ["Substitute y = 2x into x + y = 12.", "x + 2x = 12  →  3x = 12."],
            SolutionSteps = ["x + 2x = 12", "3x = 12", "x = 4"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-systems-002",
            SkillId    = "alg-systems",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Solve by elimination.  x + y = 8 and x − y = 2.  What is x?",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["Add the two equations to eliminate y.", "(x+y)+(x−y) = 8+2  →  2x = 10."],
            SolutionSteps = ["2x = 10", "x = 5"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-systems-003",
            SkillId    = "alg-systems",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Which answer is the solution of the system  y = x + 1  and  y = 3x − 3?",
            AnswerJson = """{"choices":["(2, 3)","(1, 2)","(3, 4)","(0, 1)"],"correct":0}""",
            Hints      = ["Set x + 1 = 3x − 3 and solve for x.", "x = 2, then find y."],
            SolutionSteps = ["x + 1 = 3x − 3", "4 = 2x", "x = 2, y = 3", "Answer: (2, 3)"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── alg-factoring : Factoring quadratics ─────────────────────────
        items.Add(new Item
        {
            Id         = "alg-factoring-001",
            SkillId    = "alg-factoring",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Factor completely:  x² + 5x + 6.  Choose the correct factored form.",
            AnswerJson = """{"choices":["(x+2)(x+3)","(x+1)(x+6)","(x+2)(x+4)","(x−2)(x−3)"],"correct":0}""",
            Hints      = ["Find two numbers that multiply to 6 and add to 5.", "Those numbers are 2 and 3."],
            SolutionSteps = ["Find factors of 6 that add to 5: 2 and 3.", "x² + 5x + 6 = (x+2)(x+3)"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-factoring-002",
            SkillId    = "alg-factoring",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Factor:  x² − 9.  Choose the correct factored form.",
            AnswerJson = """{"choices":["(x−3)(x+3)","(x−9)(x+1)","(x−3)²","(x+3)²"],"correct":0}""",
            Hints      = ["This is a difference of two perfect squares: a² − b² = (a−b)(a+b).", "Here a = x and b = 3."],
            SolutionSteps = ["x² − 9 = x² − 3²", "= (x−3)(x+3)"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-factoring-003",
            SkillId    = "alg-factoring",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Factor:  2x² + 7x + 3.  Choose the correct factored form.",
            AnswerJson = """{"choices":["(2x+1)(x+3)","(x+1)(2x+3)","(2x+3)(x+1)","(2x−1)(x−3)"],"correct":0}""",
            Hints      = ["Multiply the leading coefficient and constant: 2 × 3 = 6.", "Find factors of 6 that add to 7: 1 and 6. Rewrite the middle term."],
            SolutionSteps = ["2x² + x + 6x + 3", "x(2x+1) + 3(2x+1)", "(2x+1)(x+3)"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── alg-quadratic-formula : Quadratic formula ──────────────────────
        items.Add(new Item
        {
            Id         = "alg-quadratic-001",
            SkillId    = "alg-quadratic-formula",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Use the quadratic formula on  x² − 5x + 6 = 0.  Enter the larger root.",
            AnswerJson = """{"value":3,"tolerance":0.01}""",
            Hints      = ["a=1, b=−5, c=6.  Discriminant = b²−4ac = 25−24 = 1.", "x = (5 ± 1) / 2, so roots are 3 and 2."],
            SolutionSteps = ["x = (5 ± √1) / 2", "x = 3 or x = 2", "Larger root = 3"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-quadratic-002",
            SkillId    = "alg-quadratic-formula",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Solve x² + 2x − 8 = 0 using the quadratic formula.  Enter the positive root.",
            AnswerJson = """{"value":2,"tolerance":0.01}""",
            Hints      = ["a=1, b=2, c=−8.  Discriminant = 4 + 32 = 36.", "x = (−2 ± 6) / 2.  Positive root = (−2+6)/2."],
            SolutionSteps = ["x = (−2 ± √36) / 2", "x = (−2 ± 6) / 2", "Positive root = 4/2 = 2"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-quadratic-003",
            SkillId    = "alg-quadratic-formula",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "For  2x² − 4x + 2 = 0, how many distinct real roots are there?",
            AnswerJson = """{"choices":["0","1","2","Infinitely many"],"correct":1}""",
            Hints      = ["Calculate the discriminant: b² − 4ac.", "Discriminant = 16 − 16 = 0  →  exactly one (repeated) root."],
            SolutionSteps = ["Discriminant = (−4)²−4(2)(2) = 16−16 = 0", "One repeated real root."],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── alg-exponents-radicals : Exponents and radicals ───────────────
        items.Add(new Item
        {
            Id         = "alg-exponents-001",
            SkillId    = "alg-exponents-radicals",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Simplify:  √(49).  Enter the exact value.",
            AnswerJson = """{"value":7,"tolerance":0.01}""",
            Hints      = ["The square root of a perfect square is an integer.", "7 × 7 = 49."],
            SolutionSteps = ["√49 = 7"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-exponents-002",
            SkillId    = "alg-exponents-radicals",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Simplify:  x³ · x⁴.  Choose the correct answer.",
            AnswerJson = """{"choices":["x⁷","x¹²","x³⁴","2x⁷"],"correct":0}""",
            Hints      = ["When multiplying same-base powers, add the exponents.", "3 + 4 = 7."],
            SolutionSteps = ["x³ · x⁴ = x^(3+4) = x⁷"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-exponents-003",
            SkillId    = "alg-exponents-radicals",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Simplify:  √(75).  Enter the simplified radical (just the coefficient of √3, e.g. enter 5 for 5√3).",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["75 = 25 × 3, and √25 = 5.", "√75 = √(25·3) = 5√3."],
            SolutionSteps = ["√75 = √(25·3) = 5√3", "Coefficient = 5"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── alg-function-notation : Function notation ────────────────────
        items.Add(new Item
        {
            Id         = "alg-function-001",
            SkillId    = "alg-function-notation",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Given f(x) = 2x + 3, what is f(4)?",
            AnswerJson = """{"value":11,"tolerance":0.01}""",
            Hints      = ["Substitute x = 4 into the formula.", "f(4) = 2(4) + 3."],
            SolutionSteps = ["f(4) = 2(4) + 3 = 8 + 3 = 11"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-function-002",
            SkillId    = "alg-function-notation",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Given g(x) = x² − 1, what is g(−3)?",
            AnswerJson = """{"value":8,"tolerance":0.01}""",
            Hints      = ["Substitute x = −3.", "g(−3) = (−3)² − 1 = 9 − 1."],
            SolutionSteps = ["g(−3) = (−3)² − 1 = 9 − 1 = 8"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-function-003",
            SkillId    = "alg-function-notation",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Given h(x) = 3x − 5, what value of x makes h(x) = 10?",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["Set 3x − 5 = 10 and solve for x.", "Add 5 to both sides, then divide by 3."],
            SolutionSteps = ["3x − 5 = 10", "3x = 15", "x = 5"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── alg-slope-lines : Slope and linear equations ──────────────────
        items.Add(new Item
        {
            Id         = "alg-slope-001",
            SkillId    = "alg-slope-lines",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "What is the slope of the line passing through (1, 2) and (3, 8)?",
            AnswerJson = """{"value":3,"tolerance":0.01}""",
            Hints      = ["Slope m = (y₂ − y₁) / (x₂ − x₁).", "m = (8 − 2) / (3 − 1)."],
            SolutionSteps = ["m = (8−2)/(3−1) = 6/2 = 3"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-slope-002",
            SkillId    = "alg-slope-lines",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The equation of a line is y = −2x + 5.  What is the y-intercept?",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["Slope-intercept form is y = mx + b.", "b is the y-intercept."],
            SolutionSteps = ["y = −2x + 5", "b = 5"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });
        items.Add(new Item
        {
            Id         = "alg-slope-003",
            SkillId    = "alg-slope-lines",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Write the slope-intercept equation of the line through (0, −3) with slope 4.  What is the slope?",
            AnswerJson = """{"value":4,"tolerance":0.01}""",
            Hints      = ["y = mx + b. Here m = 4 and the y-intercept b = −3.", "y = 4x − 3"],
            SolutionSteps = ["y = 4x + (−3)", "Slope = 4"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── alg-distance-midpoint : Distance and midpoint formulas ────────
        items.Add(new Item
        {
            Id         = "alg-distance-001",
            SkillId    = "alg-distance-midpoint",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "What is the distance between (0, 0) and (3, 4)?",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["Use d = √((x₂−x₁)² + (y₂−y₁)²).", "d = √(9 + 16) = √25."],
            SolutionSteps = ["d = √((3−0)²+(4−0)²) = √(9+16) = √25 = 5"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-distance-002",
            SkillId    = "alg-distance-midpoint",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Find the midpoint of the segment with endpoints (2, 6) and (8, 2).  What is the x-coordinate of the midpoint?",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = ["Midpoint = ((x₁+x₂)/2, (y₁+y₂)/2).", "x = (2+8)/2."],
            SolutionSteps = ["x = (2+8)/2 = 10/2 = 5", "y = (6+2)/2 = 4", "Midpoint = (5, 4)"],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Procedural
        });
        items.Add(new Item
        {
            Id         = "alg-distance-003",
            SkillId    = "alg-distance-midpoint",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "M is the midpoint of AB.  A = (1, 3) and M = (4, 7).  What is the x-coordinate of B?",
            AnswerJson = """{"value":7,"tolerance":0.01}""",
            Hints      = ["Midpoint formula: M = ((x_A+x_B)/2, (y_A+y_B)/2).", "4 = (1 + x_B)/2  →  x_B = 7."],
            SolutionSteps = ["4 = (1 + x_B)/2", "8 = 1 + x_B", "x_B = 7"],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        return items;
    }
}
