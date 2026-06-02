# Skill 4 — Perpendicular Lines

**Skill ID:** `geo-u2-perpendicular`
**Difficulty band:** 2
**Prereqs:** geo-u2-parallel-proofs

---

## Concept

### Perpendicular Lines — Definition and Notation

Two lines (or segments, or rays) are **perpendicular** when they intersect at exactly 90°. The 90° angle is called a **right angle**, and we mark it with a small square at the intersection rather than an arc. In notation, we write **k ⊥ l** to say "line k is perpendicular to line l."

Perpendicularity is a special case of intersection: all perpendicular lines meet, but not all intersecting lines are perpendicular.

### The Right-Angle Mark

Whenever a diagram shows a small square at an intersection or at the corner of a figure, it announces a 90° angle. In a proof, citing "right angle mark" or "given ⊥" is enough to justify using 90°. Always draw the mark when you know an angle is right — omitting it is a common source of lost credit in proofs.

### The Perpendicular Bisector

A **perpendicular bisector** of a segment is a line that is both perpendicular to the segment and passes through its midpoint. Every point on the perpendicular bisector of a segment is equidistant from the two endpoints — a fact used constantly in triangle congruence proofs and in constructions.

### Perpendicular Slopes in Coordinate Geometry

On the coordinate plane, two non-vertical lines are perpendicular if and only if the product of their slopes is −1:

$$m_1 \times m_2 = -1$$

Equivalently, the slopes are **negative reciprocals** of each other: if one line has slope m, any perpendicular line has slope −1/m.

Examples:
- Slope 2 → perpendicular slope −1/2.
- Slope −3/4 → perpendicular slope 4/3.
- Slope 0 (horizontal) → perpendicular slope undefined (vertical line).

A vertical line and a horizontal line are always perpendicular, even though the slope rule cannot be stated numerically (vertical lines have undefined slope).

### Distance from a Point to a Line

The **distance from a point to a line** is the length of the perpendicular segment from the point to the line. This perpendicular segment is the unique shortest path from the point to the line — any other segment from the point to a point on the line is longer.

To find this distance on a coordinate plane:
1. Identify the line's equation and the given point.
2. Find the foot of the perpendicular (the point on the line closest to the given point).
3. Compute the length of the segment from the given point to the foot.

For simple horizontal or vertical lines, the calculation reduces to a difference of coordinates.

---

## Manipulate (interactive)

Line l is fixed and horizontal. Line k extends from fixed point Q (on l) to draggable point P.

**Drag P until ∠LQP = 90°**, making k ⊥ l. The live angle display at Q updates as you drag. When you land on 90°, a right-angle mark appears at Q and the success banner fires.

Notice that only one direction places k exactly perpendicular to l (straight up from Q, since l is horizontal). Any tilt away from vertical produces an angle other than 90°. This reflects the geometric fact that through any point on a line, there is exactly one perpendicular to that line.

---

## Worked Example

> Find the distance from point X = (2, 7) to line l: y = 3.

**Step 1 — Set up.** The distance from a point to a line is the length of the perpendicular segment from the point to the line. Because l is horizontal (y = 3), the perpendicular from X must be vertical.

**Step 2 — Find the foot.** A vertical line through X has equation x = 2. It intersects l at the point where x = 2 and y = 3, so the foot of the perpendicular is F = (2, 3).

**Step 3 — Compute the length.** The distance is:
$$XF = |7 - 3| = 4 \text{ units}$$

**Step 4 — Verify perpendicularity.** Segment XF goes from (2, 7) to (2, 3) — same x-coordinate, so XF is vertical. Line l is horizontal (slope = 0). A vertical segment and a horizontal line are perpendicular. The right-angle mark at F confirms this.

**Conclusion:** The distance from X to l is **4 units**.

---

## Reflection

Perpendicular lines appear everywhere in geometry, and recognizing them early is a force multiplier.

- **Altitudes of triangles** are perpendicular from a vertex to the opposite side — the foundation of area calculations and orthocenter proofs.
- **Perpendicular bisectors** of triangle sides converge at the circumcenter; they're also the locus of points equidistant from two endpoints.
- **Distance from a point to a line** — the formula generalizes to oblique lines using the slope relationship m₁m₂ = −1 and the point-to-line distance formula.
- **Coordinate geometry proofs** regularly require showing that two segments are perpendicular by checking that their slopes multiply to −1.

The 90° angle is geometry's most load-bearing number. When you see the small square, stop and ask: what does this right angle let me conclude? Nine times out of ten, it unlocks the next step.
