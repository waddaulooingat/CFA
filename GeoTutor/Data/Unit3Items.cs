namespace GeoTutor.Data;

using GeoTutor.Core.Models;

/// <summary>
/// Library items for Unit 3 — Triangle Congruence.
/// 3 items per skill (difficulties 1, 2, 3) × 7 skills = 21 items.
/// Used by LessonEngineService.BuildFallbackBeats when the LLM is offline.
///
/// Skills covered:
///   geo-u3-sss               — SSS congruence postulate
///   geo-u3-sas               — SAS congruence postulate
///   geo-u3-asa               — ASA congruence postulate
///   geo-u3-aas               — AAS congruence theorem
///   geo-u3-hl                — HL theorem for right triangles
///   geo-u3-cpctc             — CPCTC
///   geo-u3-congruence-proofs — Triangle congruence proofs
/// </summary>
public static class Unit3Items
{
    public static readonly IReadOnlyList<Item> All = BuildAll();

    private static List<Item> BuildAll()
    {
        var items = new List<Item>();

        // ── geo-u3-sss : SSS congruence postulate ──────────────────────────

        items.Add(new Item
        {
            Id         = "u3-sss-001",
            SkillId    = "geo-u3-sss",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "If three sides of one triangle are congruent to three corresponding sides of another triangle, the triangles are congruent by the ___ postulate.",
            AnswerJson = """{"choices":["SSS (Side-Side-Side)","SAS (Side-Angle-Side)","ASA (Angle-Side-Angle)","AAS (Angle-Angle-Side)"],"correct":0}""",
            Hints      = [
                "SSS stands for Side-Side-Side.",
                "This postulate only requires matching all three pairs of sides — no angles needed."
            ],
            SolutionSteps = [
                "The SSS Congruence Postulate states: if three sides of △ABC equal three sides of △DEF, then △ABC ≅ △DEF.",
                "Answer: SSS (Side-Side-Side)."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-sss-002",
            SkillId    = "geo-u3-sss",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△ABC has AB = 5, BC = 7, CA = 9. △DEF has DE = 5, EF = 7, FD = 9. Are the triangles congruent, and if so, by which postulate?",
            AnswerJson = """{"choices":["Yes, △ABC ≅ △DEF by SSS","Yes, △ABC ≅ △DEF by SAS","No — we also need an angle","Yes, △ABC ≅ △DEF by ASA"],"correct":0}""",
            Hints      = [
                "Compare all three pairs of sides: AB ↔ DE, BC ↔ EF, CA ↔ FD.",
                "All three pairs are equal, so SSS applies directly."
            ],
            SolutionSteps = [
                "AB = DE = 5, BC = EF = 7, CA = FD = 9.",
                "All three sides match, so SSS applies.",
                "Answer: Yes, △ABC ≅ △DEF by SSS."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-sss-003",
            SkillId    = "geo-u3-sss",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△ABC ≅ △DEF by SSS. AB = 2x + 3 and DE = 4x − 7. What is the value of x?",
            AnswerJson = """{"value":5,"tolerance":0.01}""",
            Hints      = [
                "If △ABC ≅ △DEF by SSS, then all corresponding sides are equal.",
                "AB corresponds to DE, so set 2x + 3 = 4x − 7 and solve."
            ],
            SolutionSteps = [
                "Corresponding sides are congruent: AB = DE",
                "2x + 3 = 4x − 7",
                "10 = 2x",
                "x = 5",
                "Check: AB = 13, DE = 13 ✓"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u3-sas : SAS congruence postulate ──────────────────────────

        items.Add(new Item
        {
            Id         = "u3-sas-001",
            SkillId    = "geo-u3-sas",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The SAS postulate requires two pairs of congruent sides AND a congruent angle. The angle must be ___.",
            AnswerJson = """{"choices":["Included (between the two congruent sides)","Opposite one of the sides","Any angle in the triangle","The largest angle"],"correct":0}""",
            Hints      = [
                "SAS: Side, ANGLE, Side — the angle is sandwiched between the two sides.",
                "If the angle is NOT between the two sides, you have SSA, which is NOT a valid congruence postulate."
            ],
            SolutionSteps = [
                "SAS requires the angle to be included — between the two congruent sides.",
                "Side-Angle-Side means the pattern is: side, angle, side (in order around the triangle).",
                "Answer: Included (between the two congruent sides)."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-sas-002",
            SkillId    = "geo-u3-sas",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "In △ABC and △DEF: AB = DE, ∠A ≅ ∠D, and AC = DF. Which congruence postulate applies?",
            AnswerJson = """{"choices":["SAS — ∠A is between sides AB and AC","SSS — three sides are given","ASA — two angles and the included side","AAS — two angles and a non-included side"],"correct":0}""",
            Hints      = [
                "The two sides given are AB and AC, which both meet at vertex A.",
                "∠A is between sides AB and AC — that makes ∠A the included angle."
            ],
            SolutionSteps = [
                "AB = DE (one side), ∠A ≅ ∠D (included angle at A/D), AC = DF (second side).",
                "The angle ∠A is between the two sides AB and AC → SAS.",
                "Answer: SAS."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-sas-003",
            SkillId    = "geo-u3-sas",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△RST and △XYZ have RS = XY, ST = YZ, and ∠T ≅ ∠Z. Can we conclude △RST ≅ △XYZ by SAS?",
            AnswerJson = """{"choices":["No — ∠T is not between RS and ST, so this is SSA (not valid)","Yes — two sides and an angle are congruent","Yes — this satisfies SAS","No — we need a third side"],"correct":0}""",
            Hints      = [
                "In △RST, the two congruent sides are RS and ST. What vertex is between them?",
                "Vertex S is between RS and ST, so the included angle is ∠S — not ∠T. Using ∠T gives SSA."
            ],
            SolutionSteps = [
                "The sides RS and ST share vertex S, so the included angle is ∠S.",
                "We are given ∠T ≅ ∠Z, but ∠T is NOT between RS and ST.",
                "RS, ST, ∠T follows the pattern Side-Side-Angle (SSA) — NOT a valid congruence postulate.",
                "Answer: No — this is SSA, not SAS."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-u3-asa : ASA congruence postulate ──────────────────────────

        items.Add(new Item
        {
            Id         = "u3-asa-001",
            SkillId    = "geo-u3-asa",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The ASA postulate requires two pairs of congruent angles AND a congruent side. The side must be ___.",
            AnswerJson = """{"choices":["Included (between the two congruent angles)","Opposite one of the angles","The longest side","Any side of the triangle"],"correct":0}""",
            Hints      = [
                "ASA: Angle, Side, Angle — the side is sandwiched between the two angles.",
                "Think of it as a side that has one of the congruent angles at each endpoint."
            ],
            SolutionSteps = [
                "ASA requires the side to be included — between the two congruent angles.",
                "The included side has the two congruent angles as its endpoints.",
                "Answer: Included (between the two congruent angles)."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-asa-002",
            SkillId    = "geo-u3-asa",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "In △MNP and △QRS: ∠M ≅ ∠Q, MN = QR, ∠N ≅ ∠R. Which congruence postulate applies?",
            AnswerJson = """{"choices":["ASA — MN is between ∠M and ∠N","SAS — two sides and an angle","AAS — two angles and a non-included side","SSS — all three sides"],"correct":0}""",
            Hints      = [
                "The given side is MN. Which angles are at the endpoints of MN?",
                "∠M is at vertex M and ∠N is at vertex N — both endpoints of side MN. So MN is the included side."
            ],
            SolutionSteps = [
                "MN has ∠M at one end and ∠N at the other → MN is the included side.",
                "Pattern: ∠M, MN, ∠N ↔ ∠Q, QR, ∠R → ASA.",
                "Answer: ASA."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-asa-003",
            SkillId    = "geo-u3-asa",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△ABC and △DEF have ∠A ≅ ∠D, ∠B ≅ ∠E, and BC = EF. Is this ASA or AAS?",
            AnswerJson = """{"choices":["AAS — BC is opposite ∠A, so it is a non-included side","ASA — BC is between ∠B and ∠C","ASA — any two angles with any side is ASA","AAS — BC is the shortest side"],"correct":0}""",
            Hints      = [
                "The two congruent angle pairs are ∠A and ∠B. The included side between them is AB.",
                "BC is NOT between ∠A and ∠B — it connects ∠B to the third vertex C. That makes BC a non-included side."
            ],
            SolutionSteps = [
                "Congruent angles: ∠A ≅ ∠D and ∠B ≅ ∠E.",
                "The included side between ∠A and ∠B would be AB.",
                "BC is opposite ∠A, not between ∠A and ∠B → non-included.",
                "Two angles + non-included side → AAS.",
                "Answer: AAS."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-u3-aas : AAS congruence theorem ────────────────────────────

        items.Add(new Item
        {
            Id         = "u3-aas-001",
            SkillId    = "geo-u3-aas",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The AAS theorem proves two triangles congruent using two pairs of congruent angles and a congruent side. The side must be ___.",
            AnswerJson = """{"choices":["Non-included (not between the two congruent angles)","Included (between the two congruent angles)","The hypotenuse","The longest side"],"correct":0}""",
            Hints      = [
                "AAS vs ASA: both use two angles and one side. The difference is whether the side is included.",
                "AAS uses a side that is NOT between the two congruent angles."
            ],
            SolutionSteps = [
                "AAS: Angle-Angle-Side (non-included).",
                "The side is adjacent to one angle but not between both congruent angles.",
                "Answer: Non-included (not between the two congruent angles)."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-aas-002",
            SkillId    = "geo-u3-aas",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△PQR and △STU have ∠P ≅ ∠S, ∠Q ≅ ∠T, and QR = TU. Which theorem proves the triangles congruent?",
            AnswerJson = """{"choices":["AAS — QR is opposite ∠P (non-included)","ASA — QR is the included side","SAS — two sides and an angle","SSS — all three sides"],"correct":0}""",
            Hints      = [
                "The two angles are ∠P and ∠Q. The included side between them would be PQ.",
                "QR is not between ∠P and ∠Q; it's the side from Q to R. So QR is non-included → AAS."
            ],
            SolutionSteps = [
                "∠P ≅ ∠S and ∠Q ≅ ∠T (two angle pairs).",
                "QR is between vertex Q and R; the included side for ∠P and ∠Q would be PQ.",
                "QR is non-included → Angle-Angle-Side = AAS.",
                "Answer: AAS."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-aas-003",
            SkillId    = "geo-u3-aas",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "To use AAS instead of ASA, you need two congruent angle pairs and a side. How does AAS differ from ASA in what you need to know about the side?",
            AnswerJson = """{"choices":["AAS uses a side NOT between the two angles; ASA uses a side BETWEEN the two angles","AAS uses the longest side; ASA uses the shortest side","AAS and ASA are identical — either works","AAS requires the right angle; ASA does not"],"correct":0}""",
            Hints      = [
                "Think about where the 'S' sits in each abbreviation: A-S-A vs A-A-S.",
                "In ASA, the S is in the middle (between the two A's). In AAS, the S is at the end (non-included)."
            ],
            SolutionSteps = [
                "ASA: Angle – included Side – Angle (side between the two angles).",
                "AAS: Angle – Angle – non-included Side (side not between the two angles).",
                "Both are valid congruence shortcuts, but they require different positions for the side.",
                "Answer: AAS uses a non-included side; ASA uses an included side."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        // ── geo-u3-hl : HL theorem for right triangles ─────────────────────

        items.Add(new Item
        {
            Id         = "u3-hl-001",
            SkillId    = "geo-u3-hl",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "The Hypotenuse-Leg (HL) theorem can ONLY be applied to ___ triangles.",
            AnswerJson = """{"choices":["Right triangles","Obtuse triangles","Equilateral triangles","Isosceles triangles"],"correct":0}""",
            Hints      = [
                "The 'H' in HL stands for Hypotenuse — only one type of triangle has a hypotenuse.",
                "A hypotenuse is the side opposite the right angle."
            ],
            SolutionSteps = [
                "Only right triangles have a hypotenuse (the side opposite the 90° angle).",
                "HL requires: right angle, congruent hypotenuses, and one pair of congruent legs.",
                "Answer: Right triangles."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-hl-002",
            SkillId    = "geo-u3-hl",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Right triangles △ABC and △DEF have right angles at C and F. AB = DE (hypotenuses) and AC = DF (one pair of legs). Which theorem proves the triangles congruent?",
            AnswerJson = """{"choices":["HL (Hypotenuse-Leg)","SAS — two sides and an angle","SSS — need all three sides","ASA — need two angles"],"correct":0}""",
            Hints      = [
                "Both triangles are right triangles (right angles at C and F).",
                "We have equal hypotenuses (AB = DE) and one equal leg (AC = DF). That's exactly what HL requires."
            ],
            SolutionSteps = [
                "Both triangles are right triangles — HL is applicable.",
                "Hypotenuses: AB = DE ✓",
                "One leg: AC = DF ✓",
                "HL Theorem: right triangles with congruent hypotenuse and one leg are congruent.",
                "Answer: HL."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-hl-003",
            SkillId    = "geo-u3-hl",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "Right △ABC ≅ right △DEF by HL. The hypotenuses are AB = 3x − 1 and DE = 2x + 5. What is the value of x?",
            AnswerJson = """{"value":6,"tolerance":0.01}""",
            Hints      = [
                "Congruent triangles have congruent corresponding parts.",
                "Set the hypotenuse expressions equal: 3x − 1 = 2x + 5, then solve."
            ],
            SolutionSteps = [
                "Corresponding parts of congruent triangles are congruent (CPCTC / HL).",
                "3x − 1 = 2x + 5",
                "x = 6",
                "Check: AB = 17, DE = 17 ✓"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u3-cpctc : CPCTC ───────────────────────────────────────────

        items.Add(new Item
        {
            Id         = "u3-cpctc-001",
            SkillId    = "geo-u3-cpctc",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "CPCTC stands for 'Corresponding Parts of Congruent Triangles are ___'.",
            AnswerJson = """{"choices":["Congruent","Equal in area","Similar","Proportional"],"correct":0}""",
            Hints      = [
                "The last C in CPCTC stands for the conclusion about corresponding parts.",
                "Congruent means identical in shape and size — not merely proportional."
            ],
            SolutionSteps = [
                "CPCTC: Corresponding Parts of Congruent Triangles are Congruent.",
                "Once two triangles are proven congruent, ALL corresponding parts (sides AND angles) are congruent.",
                "Answer: Congruent."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-cpctc-002",
            SkillId    = "geo-u3-cpctc",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△ABC ≅ △DEF has been proved by SAS. Which of the following can be concluded using CPCTC?",
            AnswerJson = """{"choices":["∠A ≅ ∠D","∠A ≅ ∠E","BC = DE","AC = EF"],"correct":0}""",
            Hints      = [
                "CPCTC applies to CORRESPONDING parts — vertex A corresponds to vertex D in △ABC ≅ △DEF.",
                "The letters line up: A↔D, B↔E, C↔F."
            ],
            SolutionSteps = [
                "The congruence △ABC ≅ △DEF means: A↔D, B↔E, C↔F.",
                "Corresponding angles: ∠A ↔ ∠D, ∠B ↔ ∠E, ∠C ↔ ∠F.",
                "By CPCTC: ∠A ≅ ∠D.",
                "Answer: ∠A ≅ ∠D."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-cpctc-003",
            SkillId    = "geo-u3-cpctc",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "△PQR ≅ △STU. PQ corresponds to ST. If PQ = 2x + 1 and ST = x + 8, what is the value of x?",
            AnswerJson = """{"value":7,"tolerance":0.01}""",
            Hints      = [
                "By CPCTC, corresponding sides of congruent triangles are equal.",
                "PQ = ST → 2x + 1 = x + 8. Solve for x."
            ],
            SolutionSteps = [
                "CPCTC: PQ = ST",
                "2x + 1 = x + 8",
                "x = 7",
                "Check: PQ = 15, ST = 15 ✓"
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        // ── geo-u3-congruence-proofs : Triangle congruence proofs ──────────

        items.Add(new Item
        {
            Id         = "u3-prf-001",
            SkillId    = "geo-u3-congruence-proofs",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "In a two-column proof, the right column contains ___.",
            AnswerJson = """{"choices":["Reasons (postulates, theorems, definitions)","Statements (what is true at each step)","Diagrams of the figure","Numerical calculations"],"correct":0}""",
            Hints      = [
                "A two-column proof has Statements on the left and ___ on the right.",
                "Each reason justifies the statement on the same row — it could be 'Given', a postulate, or a theorem."
            ],
            SolutionSteps = [
                "Left column: Statements (logical claims about the figure).",
                "Right column: Reasons (Given, definitions, postulates, theorems).",
                "Answer: Reasons."
            ],
            Difficulty = 1,
            Source     = "library",
            Type       = ItemType.Conceptual
        });

        items.Add(new Item
        {
            Id         = "u3-prf-002",
            SkillId    = "geo-u3-congruence-proofs",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "In a proof you are given: AB = DE, ∠B ≅ ∠E, BC = EF. Which reason proves △ABC ≅ △DEF?",
            AnswerJson = """{"choices":["SAS — ∠B is the included angle between AB and BC","SSS — three pairs of congruent parts","ASA — two angles and a side","AAS — two angles and a non-included side"],"correct":0}""",
            Hints      = [
                "List what you have: AB = DE (side), ∠B ≅ ∠E (angle), BC = EF (side).",
                "∠B is at vertex B, which is between sides AB and BC. That makes ∠B the included angle."
            ],
            SolutionSteps = [
                "Given: AB = DE (side), ∠B ≅ ∠E (angle at B/E), BC = EF (side).",
                "∠B is between sides AB and BC → included angle.",
                "Pattern: Side – included Angle – Side → SAS.",
                "Answer: SAS."
            ],
            Difficulty = 2,
            Source     = "library",
            Type       = ItemType.Procedural
        });

        items.Add(new Item
        {
            Id         = "u3-prf-003",
            SkillId    = "geo-u3-congruence-proofs",
            Template   = "text",
            ParamsJson = "{}",
            Prompt     = "To prove △ABC ≅ △DCB by SAS, you are given AB = DC and ∠ABC ≅ ∠DCB. What third statement is needed, and what is its reason?",
            AnswerJson = """{"choices":["BC = CB; Reflexive Property","BC = CB; CPCTC","∠A ≅ ∠D; Given","AC = DB; Definition of congruence"],"correct":0}""",
            Hints      = [
                "SAS requires two sides and the included angle. You have one side (AB = DC) and the included angle. What is the second side?",
                "Both triangles share the side BC — it equals itself. What property justifies that any segment equals itself?"
            ],
            SolutionSteps = [
                "Need: two sides and the included angle for SAS.",
                "Given: AB = DC (first side) and ∠ABC ≅ ∠DCB (included angle).",
                "The second side: both triangles share segment BC.",
                "BC = CB by the Reflexive Property (any segment is congruent to itself).",
                "Answer: BC = CB; Reflexive Property."
            ],
            Difficulty = 3,
            Source     = "library",
            Type       = ItemType.Transfer
        });

        return items;
    }
}
