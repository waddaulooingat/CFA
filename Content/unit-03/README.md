# GeoTutor — Unit 3 Content Pack

Unit 3: **Triangle Congruence**. Seven skill nodes: the four main congruence shortcuts (SSS, SAS, ASA, AAS), the special right-triangle shortcut (HL), CPCTC, and writing two-column congruence proofs.

## Layout

```
unit-03/
  README.md                         ← this file
  unit-03-skills.json               ← skill graph seed (7 nodes + prereq edges)
  unit-03/lessons/
    01-sss.md/.json
    02-sas.md/.json
    03-asa.md/.json
    04-aas.md/.json
    05-hl.md/.json
    06-cpctc.md/.json
    07-congruence-proofs.md/.json
```

## Scene primitives used

All Unit 3 scenes use primitives already supported by `SceneRenderer`:

| Primitive | Used for |
|-----------|----------|
| Segment (no extend) | Triangle sides |
| Mark `type:"congruenceTick"` | Marking congruent sides (groupIndex 1, 2, 3) |
| Mark `type:"rightAngle"` | Right-angle vertex in HL scenes |
| Arc `vertex/from/to` | Angle congruence marks |
| Measurement `type:"length"` | Live side-length display in manipulate beats |
| Measurement `type:"angleMeasure"` | Live angle display in manipulate beats |
| Annotation | Labels (△ABC ≅ △DEF, CPCTC, etc.) |

## Manipulate beat success conditions used

- `equalLengths` — drag a vertex until a target side matches a reference side
- `angleMeasure` — drag a vertex until an angle hits a target value

## Skill sequence

| Order | Skill ID | Name | Prereqs |
|-------|----------|------|---------|
| 1 | geo-u3-sss | SSS Congruence Postulate | geo-u1-angle-measure, geo-u2-transversal-angles |
| 2 | geo-u3-sas | SAS Congruence Postulate | geo-u3-sss |
| 3 | geo-u3-asa | ASA Congruence Postulate | geo-u3-sss |
| 4 | geo-u3-aas | AAS Congruence Theorem | geo-u3-asa |
| 5 | geo-u3-hl  | HL Theorem for Right Triangles | geo-u3-sas, geo-u2-parallel-basics |
| 6 | geo-u3-cpctc | CPCTC | geo-u3-sss, geo-u3-sas, geo-u3-asa |
| 7 | geo-u3-congruence-proofs | Triangle Congruence Proofs | geo-u3-cpctc, geo-u1-if-then-logic |
