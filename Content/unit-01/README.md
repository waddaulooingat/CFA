# GeoTutor — Unit 1 Content Pack

Unit 1: **Foundations & Logical Reasoning**. Five skill nodes with full beat sequences (Concept, Manipulate, Worked Example, Check, Practice, Reflection) and a post-lesson test bank per skill.

## Layout

```
unit-01/
  README.md                    ← this file
  unit-01-skills.json          ← skill graph seed (5 nodes + prereq edges)
  lessons/
    01-point-line-plane.md     ← human-readable prose per beat
    01-point-line-plane.json   ← machine-loaded: scenes, items, success conditions
    02-segments-and-rays.md
    02-segments-and-rays.json
    03-angle-measure.md
    03-angle-measure.json
    04-if-then-logic.md
    04-if-then-logic.json
    05-counterexamples.md
    05-counterexamples.json
```

Each lesson's `.md` and `.json` share the same beat IDs so the app can pull prose from the markdown and structured data from the JSON.

## Important constraint flag

The available render templates (Triangle, RightTriangle, IsoscelesTriangle, ParallelLinesTransversal, Circle) **do not naturally cover Unit 1 foundations**. Unit 1 is about points, lines, rays, angles, and logic — primitives that sit below the template layer.

Every scene in this pack is therefore written as a **raw scene spec** using the primitives defined in design doc §5.1 (points, segments, arcs, labels, marks, measurements, constraints). No template invocations.

**Action required for Claude Code:** the renderer must accept raw scene specs directly, not only template outputs. If it currently only consumes `{template, params}` envelopes, add a code path for raw specs. The design doc always assumed templates *produce* scene specs; the renderer's input is the spec, not the template.

## Skill graph integration

Replace any auto-generated `geo-u1-*` placeholder skills with the 5 nodes in `unit-01-skills.json`. The placeholder pattern (e.g., `geo-u1-definitions`) was the symptom of an unseeded `gt_skills` table — this fixes it.

## Beat structure recap (per design §6)

| Beat | Purpose | Where to find it |
|------|---------|------------------|
| Concept | Short prose + static diagram | `.md` prose section + `.json` `scenes.concept` |
| Manipulate | Drag-to-satisfy interactive task | `.json` `beats.manipulate` with `successCondition` |
| Worked Example | Step-by-step with canvas updates | `.md` walkthrough + `.json` `beats.worked.steps` |
| Check | 1–2 inline items, no hints | `.json` `beats.check.items` |
| Practice | 3–5 items with hints | `.json` `beats.practice.items` |
| Reflection | Plain-language recap | `.md` reflection section only |

## Test items

Each lesson JSON has a `testItems` array with 6–8 items, balanced ~40/30/30 across procedural / conceptual / transfer per design §8.

## Next units

Once Unit 1 renders end-to-end with this content, Units 2–3 follow the same format. Unit 2 (Parallel Lines & Transversals) will exercise the `ParallelLinesTransversal` template directly. Unit 3 (Triangle Congruence) will exercise `GenericTriangle` and `RightTriangle`. That's when the template library starts pulling its weight.
