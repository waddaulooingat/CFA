# Unit 02 — Parallel Lines & Transversals

**Content pack version:** 1  
**Skills:** 5 (`geo-u2-parallel-identify` → `geo-u2-angle-relationships`)  
**Prereqs:** geo-u1-angle-measure (for the first skill)

---

## Scene Primitives Used

All Unit 2 scenes use only primitives already supported by `SceneRenderer`. No new renderer work is required.

| Primitive | Used for |
|-----------|----------|
| Segment `extend:"both"` | Infinite lines (parallel lines, transversals) |
| Segment `extend:"endOnly"` | Rays from intersection points |
| Segment (no extend) | Finite reference segments |
| Mark `type:"parallelArrow"` | Indicating parallel lines (single or double chevron) |
| Mark `type:"rightAngle"` | Perpendicular line scenes |
| Arc `vertex/from/to` | Angle marks at intersections |
| Measurement `type:"angleMeasure"` | Live angle display during manipulate beats |
| Measurement `type:"length"` | Distance display (perpendicular distance lesson) |
| Annotation | Static labels for angle names (∠1, ∠2, …) |

---

## Skill Sequence

| Order | Skill ID | Name | Prereqs |
|-------|----------|------|---------|
| 1 | geo-u2-parallel-identify | Identifying parallel lines | geo-u1-angle-measure |
| 2 | geo-u2-transversal-angles | Transversal angle pairs | geo-u2-parallel-identify |
| 3 | geo-u2-parallel-proofs | Proving lines parallel | geo-u2-transversal-angles, geo-u1-if-then-logic |
| 4 | geo-u2-perpendicular | Perpendicular lines | geo-u2-parallel-identify |
| 5 | geo-u2-angle-relationships | Angle pair relationships | geo-u2-transversal-angles |

---

## Manipulate Beat Success Conditions

All Unit 2 manipulate beats use the `angleMeasure` success condition type (dragging a
point to achieve a target angle). The `constrainToSegment` hint in some scenes is
informational only — the renderer does not enforce it, but the success window is wide
enough that free dragging still reaches it comfortably.
