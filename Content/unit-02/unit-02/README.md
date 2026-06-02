# GeoTutor — Unit 2 Content Pack

Unit 2: **Parallel Lines & Transversals**. Five skill nodes covering parallel/perpendicular vocabulary, the eight angles formed by a transversal, the parallel-line theorems, their converses (proving lines parallel), and parallel/perpendicular slope relationships in the coordinate plane.

## Layout

```
unit-02/
  README.md                       ← this file
  unit-02-skills.json             ← skill graph seed (5 nodes + prereq edges)
  lessons/
    01-parallel-basics.md/.json
    02-transversal-angles.md/.json
    03-parallel-theorems.md/.json
    04-proving-parallel.md/.json
    05-coordinate-parallel.md/.json
```

## Integration notes for Claude Code

1. **Seed `gt_skills`** from `unit-02-skills.json`. Skill IDs use the `geo-u2-*` prefix and prereqs cross-reference Unit 1 IDs (`geo-u1-angle-measure`, `geo-u1-if-then-logic`, `geo-u1-point-line-plane`). Unit 1 must be seeded first.

2. **New scene primitives Unit 2 uses.** Some go beyond what Unit 1 needed. The renderer may need to extend support:
   - `parallelMarks` on segments — arrowheads (single, double) drawn on a segment to indicate which lines are parallel as a set
   - `numberedAngles` — labeling the 8 angles formed by a transversal as ∠1 through ∠8
   - `angleCongruenceMarks` — tick marks on arcs (single, double) showing which angles are congruent
   - `slope` measurement on a segment
   - `coordinateGrid` viewport flag with visible axes and grid lines

   If any of these are unsupported, the renderer should log a warning and skip the element, not bail on the whole scene (per the Unit 1 fix already discussed).

3. **Templates pull weight here.** Several scenes can be authored as `{template: "ParallelLinesTransversal", params: {...}}` envelopes once the template is implemented. For now, every scene is a raw spec so this unit works with the same renderer code path Unit 1 uses.

4. **Linear pair / vertical / supplementary terminology** — the lesson assumes these terms are known. They were touched on in Unit 1 (angle types and the practice item about supplementary angles) but not formally introduced as a skill node. If Nayla wobbles, that's the gap. Consider adding a "supplementary, complementary, vertical, linear pair" remedial node between Units 1 and 2 in a later pass.

## Beat structure recap

| Beat | Purpose |
|---|---|
| Concept | Short prose + static diagram |
| Manipulate | Drag-to-satisfy interactive task |
| Worked Example | Step-by-step with canvas updates |
| Check | 1–2 inline items |
| Practice | 3–5 items with hints |
| Reflection | Plain-language recap |

Each lesson also has 6–8 `testItems` balanced ~40/30/30 procedural / conceptual / transfer.

## What's next

Unit 3 (Triangle Congruence) is the natural follow-on. It exercises `GenericTriangle` and `RightTriangle` templates heavily and introduces the SSS/SAS/ASA/AAS/HL postulates — the start of two-column proof territory.
