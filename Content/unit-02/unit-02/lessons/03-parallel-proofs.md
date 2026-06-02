# Skill 3 — Proving Lines Parallel

**Skill ID:** `geo-u2-parallel-proofs`
**Difficulty band:** 2
**Prereqs:** geo-u2-parallel-identify

---

## Concept

So far you've moved in one direction: *given* that lines are parallel, *find* the angles. Now you reverse the arrow. Given angle information, *prove* that lines are parallel. This is the Converse direction, and it requires a different set of theorems.

### The Four Converse Theorems

Each angle-relationship theorem has a converse. Where the original says "parallel lines → angle property," the converse says "angle property → lines are parallel."

| Original Theorem | Converse (proves parallel) |
|---|---|
| If p ∥ q, then corresponding angles are congruent. | If corresponding angles are congruent, then p ∥ q. |
| If p ∥ q, then alternate interior angles are congruent. | If alternate interior angles are congruent, then p ∥ q. |
| If p ∥ q, then alternate exterior angles are congruent. | If alternate exterior angles are congruent, then p ∥ q. |
| If p ∥ q, then co-interior angles are supplementary. | If co-interior angles are supplementary, then p ∥ q. |

In everyday language: any one of these four angle conditions is **enough** to guarantee two lines are parallel. You only need to verify one.

### Direction Matters

The original theorems and their converses look similar but work in opposite logical directions.

- **Original (forward):** You *know* the lines are parallel. You *conclude* something about angles.
- **Converse (backward):** You *know* something about angles. You *conclude* the lines are parallel.

Mixing up the direction is a common error. If someone says "the lines look parallel because alternate interior angles are equal," that's correct *only if* you've actually measured or proven the angle congruence first. The parallelism doesn't prove itself from the picture.

### Logical Structure

The converse of a true theorem is **not automatically true** — but all four angle converses above happen to be true and are accepted as theorems. Their truth can be understood through the contrapositive: if lines are *not* parallel, the transversal hits them at *different* angles, so corresponding angles cannot be equal. Flip that logic and you get the converse.

### Using a Converse in a Proof

To prove lines parallel, the structure is always the same:
1. State what angle information is *given*.
2. Identify which angle relationship applies (corresponding, alternate interior, etc.).
3. Cite the appropriate **Converse** theorem.
4. Conclude the lines are parallel.

---

## Manipulate (interactive)

Line m is fixed and horizontal. Line n's direction from point F is controlled by draggable point G. The transversal connects E (on m) to F (on n).

**Drag G until ∠EFG = 90°.** Watch the live angle measurement at F. When you reach 90°, the alternate interior angles at E and F are both 90° and equal — the Converse of the AIA Theorem fires and the lines are confirmed parallel.

Notice: the diagram won't "snap" to parallel until the angle condition is exactly right. This is the key insight — **parallelism is a consequence of the angle condition**, not the other way around.

---

## Worked Example

> Given: In the figure, lines m and n are cut by transversal t at points E and F respectively. ∠AEF ≅ ∠EFD. Prove m ∥ n.

**Setup:** Label the figure. E is on m; F is on n. ∠AEF is the angle on the left side of the transversal at E, between ray EA and segment EF. ∠EFD is the angle on the right side of the transversal at F, between segment FE and ray FD. These are on **opposite sides** of the transversal and **between** the two lines — alternate interior angles.

**Two-column proof:**

| Statement | Reason |
|---|---|
| ∠AEF ≅ ∠EFD | Given |
| ∠AEF and ∠EFD are alternate interior angles | Definition: opposite sides of transversal, between the lines |
| m ∥ n | Converse of the Alternate Interior Angles Theorem |

**Verification:** Because m ∥ n, corresponding angles ∠AEB ≅ ∠EFG (both upper-right positions). Also, co-interior angles ∠BEF and ∠EFD are supplementary — their sum is 180°. Both checks confirm the result.

---

## Reflection

The forward and converse directions in geometry are genuinely different arguments. Knowing that sounds obvious, but it's one of the most frequently lost points on proofs: citing "Alternate Interior Angles Theorem" when you mean its converse costs the step and the logic.

A clean habit: before writing any theorem name in a proof, ask yourself — *am I starting with parallel lines (forward) or am I trying to prove parallel lines (converse)?* That single question will keep your direction straight.

This skill is the entry point for all two-column proofs involving parallel lines. Every subsequent proof that involves parallel lines — whether in triangles, quadrilaterals, or coordinate geometry — will call on one of these four converses or their originals.
