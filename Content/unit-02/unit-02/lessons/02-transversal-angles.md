# Skill 2 — Angles Formed by a Transversal

**Skill ID:** `geo-u2-transversal-angles`
**Difficulty band:** 2
**Prereqs:** geo-u2-parallel-identify, geo-u1-angle-measure

---

## Concept

When a transversal crosses two parallel lines, it creates **eight angles** — four at each intersection. Every one of those eight angles is related to every other by one of four theorems. Memorize the four relationships and you can find any angle in the figure from a single given angle.

**Numbering convention.** Label the angles at the upper intersection ∠1 through ∠4 (clockwise from upper-right: ∠1 upper-right, ∠2 upper-left, ∠3 lower-left, ∠4 lower-right) and the angles at the lower intersection ∠5 through ∠8 in the same pattern. The numbering is a convention, not a law — problems will often use different labels — but the *positions* are what matter.

**The four angle-pair types:**

| Pair name | Position | Example | Relationship |
|---|---|---|---|
| **Corresponding** | Same corner at each intersection | ∠1 and ∠5 | Congruent |
| **Alternate interior** | Between the lines, opposite sides of transversal | ∠3 and ∠5 | Congruent |
| **Alternate exterior** | Outside the lines, opposite sides of transversal | ∠1 and ∠7 | Congruent |
| **Co-interior (same-side interior)** | Between the lines, same side of transversal | ∠4 and ∠5 | Supplementary (sum 180°) |

**The theorems — with their formal names:**

- **Corresponding Angles Postulate:** If two parallel lines are cut by a transversal, then corresponding angles are congruent.
- **Alternate Interior Angles Theorem:** If two parallel lines are cut by a transversal, then alternate interior angles are congruent. *(Proof: each alternate interior angle equals its corresponding angle, and corresponding angles at each intersection are vertical to alternate interior angles.)*
- **Alternate Exterior Angles Theorem:** If two parallel lines are cut by a transversal, then alternate exterior angles are congruent. *(Same logic, applied to the exterior angles.)*
- **Co-interior Angles Theorem (Same-Side Interior Angles Theorem):** If two parallel lines are cut by a transversal, then co-interior angles are supplementary. *(Proof: each co-interior angle pairs with the same corresponding angle, and the two angles at one intersection are a linear pair summing to 180°.)*

**The converses hold too.** If a transversal cuts two lines and creates congruent corresponding angles (or congruent alternate interior angles, or supplementary co-interior angles), then the lines are parallel. This will be your primary tool for proving lines parallel in later units.

---

## Manipulate (interactive)

In the canvas you will see:
- Line **p** — a fixed horizontal line at the top, with point **E** on it (the intersection with the transversal).
- Line **q** — a fixed horizontal line at the bottom, parallel to p.
- A transversal passing through **E** with its far tip at draggable point **D**.

**Drag D** to rotate the transversal about E. Watch the live readout of **∠1** — the angle measured at E between the right arm of p and the transversal going toward D.

Your goal: **set ∠1 = 50°.**

Once you achieve 50°, look at where the transversal crosses line q (point F, computed automatically). The alternate interior angle at F — ∠3 at E's mirror position at F — is also 50°. The co-interior angle at F on the same side is 180° − 50° = 130°. All eight angles are determined by that single 50°.

*Takeaway:* one angle controls them all. That's the power of parallel lines.

---

## Worked Example

> Two parallel lines p and q are cut by transversal t. The transversal meets p at E and q at F. The upper-right angle at E (∠1) measures **65°**. Find the measures of all eight angles.

**Step 1.** At intersection E, use the Linear Pair Postulate.
$\angle 1 = 65°$. ∠2 (upper-left) is a linear pair with ∠1:
$$m\angle 2 = 180° - 65° = 115°.$$

**Step 2.** At intersection E, use vertical angles.
∠3 (lower-left) is vertical to ∠1: $m\angle 3 = 65°$.
∠4 (lower-right) is vertical to ∠2: $m\angle 4 = 115°$.

**Step 3.** Apply the Corresponding Angles Postulate ($p \parallel q$).
Each angle at F corresponds to the same-positioned angle at E:
$$m\angle 5 = m\angle 1 = 65°, \quad m\angle 6 = m\angle 2 = 115°,$$
$$m\angle 7 = m\angle 3 = 65°, \quad m\angle 8 = m\angle 4 = 115°.$$

**Step 4.** Verify with the other theorems.
- *Alternate interior:* ∠3 (lower-left at E) and ∠5 (upper-right at F) are on opposite sides of the transversal between the lines: $65° = 65°$ ✓
- *Co-interior:* ∠4 (lower-right at E) and ∠5 (upper-right at F) are on the same side: $115° + 65° = 180°$ ✓
- *Alternate exterior:* ∠1 (upper-right at E) and ∠7 (lower-left at F) are on opposite sides outside the lines: $65° = 65°$ ✓

**Answer:** The four 65° angles are ∠1, ∠3, ∠5, ∠7. The four 115° angles are ∠2, ∠4, ∠6, ∠8.

*Key pattern:* with parallel lines and a transversal, you always get exactly two distinct angle measures that are supplementary to each other. Every angle in the figure is one or the other.

---

## Reflection

Why spend a lesson memorizing four angle-pair names? Because they are the engine of nearly every proof and calculation involving parallel lines — and parallel lines appear everywhere.

In **triangle proofs**, the fact that the angle sum of a triangle is 180° is proven by drawing a line through one vertex parallel to the opposite side, then using alternate interior angles. In **polygon theorems**, the exterior angle sum of any convex polygon is 360°, another result built on the parallel-lines angle theorems. In **coordinate geometry**, parallel lines have equal slopes precisely *because* equal slopes produce congruent corresponding angles with the x-axis as transversal.

When you encounter a proof that says "since $p \parallel q$, we have ∠3 = ∠5 by the Alternate Interior Angles Theorem," you need to identify those angles instantly. The investment of time now — learning which pair is which, and which relationship each obeys — pays dividends in every proof you write for the rest of the course.
