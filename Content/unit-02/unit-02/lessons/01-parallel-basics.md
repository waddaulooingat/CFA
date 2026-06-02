# Skill 1 — Parallel and Perpendicular Lines

**Skill ID:** `geo-u2-parallel-basics`
**Difficulty band:** 1
**Prereqs:** geo-u1-point-line-plane

---

## Concept

Two lines in the same plane have exactly two possibilities: they either intersect at one point, or they don't intersect at all. The second case is what we call **parallel**.

**Parallel lines** lie in the same plane and never intersect, no matter how far they're extended in either direction. Notation: $\overleftrightarrow{AB} \parallel \overleftrightarrow{CD}$. We mark parallel lines on a diagram with matching arrowheads — single arrows for one pair, double arrows for a second pair.

**Perpendicular lines** intersect at a right angle (90°). Notation: $\overleftrightarrow{AB} \perp \overleftrightarrow{CD}$. We mark perpendicular lines on a diagram with a small square at the intersection.

**Skew lines** are lines that do not lie in the same plane and do not intersect. Skew only exists in 3D — there are no skew lines in a flat diagram. Think of an edge of the ceiling running east-west and an edge of the floor running north-south at different corners of a room. They never meet and they aren't parallel because they aren't coplanar.

A summary you'll use constantly:

| Relationship | Coplanar? | Intersect? | Angle |
|---|---|---|---|
| Parallel | yes | no | — |
| Perpendicular | yes | yes | 90° |
| Intersecting (non-perpendicular) | yes | yes | not 90° |
| Skew | no | no | — |

**Parallel Postulate.** Through a point not on a given line, there is exactly one line parallel to the given line. This is one of the most important postulates in geometry — every theorem about parallel lines ultimately depends on it.

**Perpendicular Postulate.** Through a point not on a given line, there is exactly one line perpendicular to the given line.

---

## Manipulate (interactive)

You'll see line $\ell$ on the canvas and a second line $m$ passing through a fixed point *P*. The line $m$ pivots around *P*. **Drag the rotation handle on $m$ until $m$ is parallel to $\ell$.**

The angle between $m$ and $\ell$ displays live. When the lines are parallel (within a degree of slop), parallel arrows appear on both lines and the success message fires.

*Why this matters:* parallel doesn't mean "looks parallel." It means a specific geometric condition. Getting the feel for that condition before the theorems land makes the theorems make sense.

---

## Worked Example

> Lines $a$ and $b$ are perpendicular. Lines $b$ and $c$ are perpendicular. What is the relationship between $a$ and $c$?

**Step 1.** Draw the picture mentally. Line $a$ meets line $b$ at 90°. Line $c$ also meets line $b$ at 90°.

**Step 2.** Two lines that both meet the same line at 90° must be parallel to each other — because there's only one direction perpendicular to $b$ in a plane, and both $a$ and $c$ point that way.

**Step 3.** Therefore $a \parallel c$.

**Answer:** $a \parallel c$. Two lines perpendicular to the same line (in a plane) are parallel.

This is a small theorem in its own right and it'll come back. Memorize the picture: two parallel rails, both perpendicular to the same crossbar.

---

## Reflection

Parallel, perpendicular, skew. Two notations: $\parallel$ and $\perp$. The Parallel Postulate fixes parallel as a *unique* relationship — only one parallel through a given outside point.

The most useful corollary from this lesson: two lines perpendicular to the same line (in a plane) are parallel. It comes up later in coordinate geometry and in two-column proofs.

Next up: when a third line cuts across two parallel lines, eight angles are formed. We need names for all of them before we can talk about which are congruent.
