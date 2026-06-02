# Skill 1 — Identifying Parallel Lines

**Skill ID:** `geo-u2-parallel-identify`
**Difficulty band:** 1
**Prereqs:** geo-u1-point-line-plane, geo-u1-angle-measure

---

## Concept

Two lines in the same plane that **never intersect** are called **parallel lines**. We write $l_1 \parallel l_2$ to say "line $l_1$ is parallel to line $l_2$." The single-bar symbol $\parallel$ is always paired with two lines; the double-bar symbol $\perp$ (which you will see soon) signals perpendicularity.

**A key requirement:** parallel lines must be *coplanar* — they must lie in the same plane. Two lines that do not intersect but are in *different* planes are called **skew lines**. Skew lines are not parallel. Picture the edge of a ceiling and the edge of the opposite wall: they never meet, but they aren't parallel because they live in different planes.

**Notation reminders.**
- $l_1 \parallel l_2$ — the lines are parallel.
- Arrows drawn on line diagrams (like single or double tick-marks on segments) indicate which lines are parallel to which. A single arrow on line $p$ and a single arrow on line $q$ means $p \parallel q$.
- $l_1 \perp l_2$ — the lines are perpendicular (meet at 90°). You'll need this symbol soon.

**Transversals.** A **transversal** is any line that intersects two or more other lines at distinct points. When a transversal cuts two parallel lines, it creates a family of angle pairs with predictable relationships — corresponding angles, alternate interior angles, co-interior angles, and more. Those relationships are the subject of the next lesson; here, the goal is recognizing and naming parallelism itself.

**Real-world parallels.**
- Railroad tracks are parallel lines; the rail ties are transversals.
- Horizontal lines on ruled notebook paper are parallel; the margin line is a transversal.
- Rows of crops in a field, lanes on a highway, the rungs of a ladder — everywhere you look, parallel lines organize the world.

---

## Manipulate (interactive)

In the canvas you'll see:
- Line **p** — a fixed horizontal line.
- A vertical transversal crossing p at point **B**.
- Line **q** — a ray starting at B and passing through draggable point **C**.

Your goal: **drag C until line q is parallel to p.**

Because p is horizontal and the transversal is vertical, the transversal makes a 90° angle with p. For q to be parallel to p, the transversal must also make a 90° angle with q — which means you need $m\angle ABC = 90°$. Watch the live angle readout and drag C downward or upward until the measure snaps to 90°.

*Takeaway:* two lines are parallel when a transversal makes the same angle with each of them. That equal-angle condition is the core idea behind all the parallel-lines theorems you'll study next.

---

## Worked Example

> Lines p and q are parallel ($p \parallel q$). A transversal crosses p at point E and q at point F. The angle in the upper-right position at E (call it $\angle 1$) measures 55°. Find the measures of the corresponding angle at F and the co-interior angle at F that shares a side with $\angle 4$.

**Step 1.** Identify the given. $m\angle 1 = 55°$ at the intersection E.

**Step 2.** Locate the corresponding angle at F. $\angle 5$ is in the upper-right position at F — the same corner as $\angle 1$. By the **Corresponding Angles Postulate** (when parallel lines are cut by a transversal, corresponding angles are congruent): $m\angle 5 = m\angle 1 = 55°$.

**Step 3.** Find the co-interior angle. The interior angles between the parallel lines on the right side of the transversal are $\angle 4$ (lower-right at E) and $\angle 6$ (upper-left at F). By the **Co-interior Angles Theorem**, $m\angle 4 + m\angle 6 = 180°$.

**Step 4.** $\angle 1$ and $\angle 4$ are vertical angles at E, so $m\angle 4 = m\angle 1 = 55°$. Wait — that's not right: $\angle 1$ (upper-right) and $\angle 4$ (lower-right) are a linear pair with $\angle 3$. Actually: $\angle 1$ (upper-right) and $\angle 3$ (lower-left) are vertical angles, so $m\angle 3 = 55°$. And $\angle 4$ (lower-right) is a linear pair with $\angle 1$: $m\angle 4 = 180° - 55° = 125°$. Co-interior: $m\angle 4 + m\angle 6 = 180°$, so $m\angle 6 = 180° - 125° = 55°$.

**Answer:** Corresponding angle $m\angle 5 = 55°$; co-interior angle $m\angle 6 = 55°$ (because $\angle 4 = 125°$ and $125° + 55° = 180°$).

---

## Reflection

Parallel lines seem simple — just lines that never meet — but they are load-bearing in geometry. The **Triangle Midsegment Theorem** (a segment connecting midpoints of two sides of a triangle is parallel to the third side) depends on this definition. In coordinate geometry, parallel lines are exactly those with equal slopes, which is why the slope-intercept form $y = mx + b$ is so powerful. Later, when you write two-column proofs, stating "$p \parallel q$" will unlock a whole set of angle theorems as reasons.

The notational habits you build now — $\parallel$ versus $\perp$, arrows on diagrams, distinguishing skew from parallel — will prevent errors in every proof and problem that involves lines for the rest of the course.
