# Skill 5 — Parallel & Perpendicular in the Coordinate Plane

**Skill ID:** `geo-u2-coordinate-parallel`
**Difficulty band:** 2
**Prereqs:** geo-u2-parallel-basics (algebra prereq: slope of a line)

---

## Concept

In the coordinate plane, parallel and perpendicular have crisp algebraic tests — no angle measurements required.

**Slope review.** The slope of a line through points $(x_1, y_1)$ and $(x_2, y_2)$ is
$$m = \frac{y_2 - y_1}{x_2 - x_1}.$$
Rise over run. Positive slope rises left-to-right, negative falls, zero is horizontal, undefined (vertical) is a vertical line.

**Parallel lines have equal slopes.** If line 1 has slope $m_1$ and line 2 has slope $m_2$, then
$$\text{line 1} \parallel \text{line 2} \iff m_1 = m_2.$$
(With one caveat: the same slope but the *same* y-intercept means it's the same line, not two parallel lines.)

**Perpendicular lines have slopes that are negative reciprocals.** That means $m_1 \cdot m_2 = -1$, or equivalently $m_2 = -\frac{1}{m_1}$.

Quick examples:
- Slopes $\tfrac{2}{3}$ and $\tfrac{2}{3}$: **parallel**.
- Slopes $\tfrac{2}{3}$ and $-\tfrac{3}{2}$: **perpendicular** (product is $-1$).
- Slopes 4 and $-\tfrac{1}{4}$: **perpendicular**.
- Slopes 5 and $-5$: NOT perpendicular ($5 \cdot -5 = -25$, not $-1$). They just go opposite ways at the same steepness.

**The special cases.**
- Horizontal lines have slope **0**; vertical lines have **undefined** slope.
- A horizontal and a vertical line are always perpendicular (you can't compute $0 \cdot \text{undefined}$, but visually they meet at 90°).
- Two horizontal lines are parallel; two vertical lines are parallel.

**Finding equations.** Given a line and a point not on it, you can write the equation of the line through that point that's parallel or perpendicular to the given line.

To find a parallel line: keep the slope; substitute the point into point-slope form.
To find a perpendicular line: take the negative reciprocal of the slope; substitute the point.

---

## Manipulate (interactive)

You'll see a fixed line $\ell$ on a coordinate grid with a clearly displayed slope. A second line passes through a fixed point *P* and is draggable — you control its slope by dragging a handle.

**Drag the handle until the second line is parallel to $\ell$.** The slope display updates live. When the two slopes match (within rounding), parallel arrows appear on both lines.

A toggle switches the task to **perpendicular**: same setup, but you drag until the slopes are negative reciprocals (the canvas highlights when $m_1 \cdot m_2 = -1$).

*Why this matters:* this is the single most common coordinate-geometry move on the SAT, the ACT, and on every honors test through Unit 11. Make it fast.

---

## Worked Example

> Find the equation of the line through point $(3, -2)$ that is **perpendicular** to the line $y = \tfrac{1}{2}x + 4$.

**Step 1.** Identify the given line's slope. From $y = \tfrac{1}{2}x + 4$, the slope is $m_1 = \tfrac{1}{2}$.

**Step 2.** Compute the perpendicular slope. The negative reciprocal of $\tfrac{1}{2}$ is $-2$. So $m_2 = -2$.

**Step 3.** Use point-slope form with the point $(3, -2)$ and slope $-2$:
$$y - (-2) = -2(x - 3)$$
$$y + 2 = -2x + 6$$
$$y = -2x + 4.$$

**Step 4.** Check. Slope of the new line is $-2$. Product with original slope: $-2 \cdot \tfrac{1}{2} = -1$. ✓ The point $(3, -2)$: $y = -2(3) + 4 = -2$. ✓

**Answer:** $y = -2x + 4$.

For the parallel version of this problem (same point, same original line, but parallel instead of perpendicular): keep slope $\tfrac{1}{2}$; substitute the point to get $y = \tfrac{1}{2}x - \tfrac{7}{2}$.

---

## Reflection

Two algebraic tests, one for each relationship:
- **Parallel:** equal slopes ($m_1 = m_2$).
- **Perpendicular:** negative reciprocal slopes ($m_1 \cdot m_2 = -1$).

Special cases: horizontal/vertical (slope 0 vs undefined) are perpendicular by inspection, not by the product test.

To build the equation of a line parallel or perpendicular to a given line through a given point: get the new slope (same, or negative reciprocal), use point-slope form, simplify.

This wraps Unit 2. You can now identify parallel lines from angle information OR from slope, prove lines parallel either way, and build new lines that fit a relationship. Unit 3 is triangle congruence — proofs start in earnest there.
