# Skill 5 — Angle Relationships in Multi-Line Figures

**Skill ID:** `geo-u2-angle-relationships`
**Difficulty band:** 3
**Prereqs:** geo-u2-perpendicular

---

## Concept

### The Big Picture: Angles Are All Connected

When a single transversal crosses two parallel lines, exactly two distinct angle measures appear in the entire figure. Call them α and (180° − α). Every one of the eight angles is either α or its supplement. Once you know one angle, you know all eight.

When two transversals cross three (or more) parallel lines, the figure looks more complicated but the logic is the same: every angle traces back to a small set of seed measures through a chain of theorems.

### The Five Relationships — A Quick Reference

| Pair type | Position | When lines ∥ |
|---|---|---|
| Vertical angles | Same intersection, opposite sides | Always equal (no parallel needed) |
| Linear pair | Same intersection, adjacent on a line | Always supplementary (no parallel needed) |
| Corresponding angles | Same position at different intersections | Equal |
| Alternate interior angles | Between lines, opposite sides of transversal | Equal |
| Co-interior (same-side interior) angles | Between lines, same side of transversal | Supplementary (sum = 180°) |

Alternate exterior angles (outside both lines, opposite sides) are also equal when lines are parallel, mirroring alternate interior angles.

### Working Systematically in Complex Figures

When a figure has multiple parallel lines and multiple transversals, resist the urge to jump to the answer. A reliable process:

1. **Label everything.** Assign numbers or letters to every angle you will need. Small labeled dots beat re-reading the problem three times.
2. **Identify known angles.** Circle or highlight every angle whose measure is given or can be directly read.
3. **Apply single-step theorems first.** Vertical angles and linear pairs give you four angles from one. Write those down.
4. **Cross intersections with parallel-line theorems.** Corresponding, alternate interior, or co-interior angles transfer a measure from one intersection to another.
5. **Repeat until the target angle is found.** Each step cites exactly one theorem.
6. **Verify.** Check that all linear pairs sum to 180° and all vertical pairs are equal.

This process turns a maze of lines into a sequence of single-step deductions.

### Common Errors

- **Forgetting to check which intersection.** Corresponding angles require the same relative position at *different* intersections on the *same* pair of parallel lines. Mixing intersections from different pairs of parallel lines is a frequent slip in three-line figures.
- **Using a parallel-line theorem when lines are not stated as parallel.** Corresponding angles are only guaranteed equal when the lines are parallel. In a figure without the parallel mark (arrows), you must not assume it.
- **Mislabeling co-interior vs alternate interior.** Both involve angles between the parallel lines. The difference is the side of the transversal: *same* side = co-interior (supplementary); *opposite* sides = alternate interior (equal).

---

## Manipulate (interactive)

Lines m and n are fixed and horizontal (parallel). The transversal passes through fixed point B on m and extends to draggable point P below.

**Drag P until ∠ABP = 40°.** Point A is the reference arm to the left of B on line m.

Once you hit 40°, trace through the figure:
- The linear pair partner to ∠ABP at B is 180° − 40° = 140°.
- The vertical angle to ∠ABP at B is 40°.
- The vertical angle to 140° at B is 140°.
- By Corresponding Angles, the angle in the same position at the lower intersection with n is also 40°.
- Its linear pair partner there is 140°.
- Its vertical angle is 40°, and the vertical to that is 140°.

All eight angles resolve to 40° or 140°. One number unlocks the whole figure.

---

## Worked Example

> Lines p ∥ q are cut by transversal t at intersections E (on p) and F (on q). m∠2 = 115°, where ∠2 is the upper-left angle at E. Find all other angles at E and F.

**Setup:** Label all eight angles: ∠1 through ∠4 at E (upper-right, upper-left, lower-left, lower-right), ∠5 through ∠8 at F (same ordering).

**Step 1 — Linear pair at E:** ∠1 and ∠2 share the straight line p, so they form a linear pair.
$$m\angle 1 = 180° - 115° = 65°$$

**Step 2 — Vertical angles at E:**
$$m\angle 3 = m\angle 1 = 65° \qquad m\angle 4 = m\angle 2 = 115°$$

At E: ∠1 = 65°, ∠2 = 115°, ∠3 = 65°, ∠4 = 115°.

**Step 3 — Corresponding Angles (p ∥ q):** Each angle at F matches its counterpart at E.
$$m\angle 5 = 65°, \quad m\angle 6 = 115°, \quad m\angle 7 = 65°, \quad m\angle 8 = 115°$$

**Step 4 — Cross-checks:**
- Alternate interior angles: ∠3 (lower-left at E) and ∠5 (upper-right at F) are on opposite sides of the transversal, between the lines → both 65° ✓.
- Co-interior angles on the right side: ∠4 (lower-right at E) and ∠5 (upper-right at F) are on the same side → 115° + 65° = 180° ✓.
- Co-interior angles on the left side: ∠3 (lower-left at E) and ∠6 (upper-left at F) are on the same side → 65° + 115° = 180° ✓.

**Result:** ∠1 = ∠3 = ∠5 = ∠7 = 65°; ∠2 = ∠4 = ∠6 = ∠8 = 115°.

---

## Reflection

Angle relationships in parallel-line figures are the backbone of nearly every subsequent proof in geometry:

- **Triangle angle sum.** The classic proof draws a line through one vertex parallel to the opposite side, then uses alternate interior and co-interior angles to show the three angles sum to 180°.
- **Polygon exterior angle theorem.** Derived by extending sides and repeatedly applying linear pairs and parallel-line angle relationships.
- **Coordinate geometry.** Proving that a quadrilateral is a parallelogram often reduces to showing angle relationships consistent with parallel sides.

The key habit is **systematic labeling before calculating**. In competition math and on standardized tests, angle-chain problems are won or lost at the labeling stage. A well-labeled diagram means every step writes itself.

Think of parallel lines as a copy machine for angles: once you know one angle at one intersection, the machine replicates it (or its supplement) at every other intersection on the same pair of parallel lines.
