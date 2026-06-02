# Skill 4 — Proving Lines Parallel

**Skill ID:** `geo-u2-proving-parallel`
**Difficulty band:** 2
**Prereqs:** geo-u2-parallel-theorems, geo-u1-if-then-logic

---

## Concept

The previous skill said: *if lines are parallel, then certain angle pairs are congruent (or supplementary)*. This skill is the **converse**: *if those angle pairs are congruent (or supplementary), then the lines are parallel*.

For each theorem from Lesson 3, there's a converse that runs the other direction. Each converse is also a theorem.

**Converse of the Corresponding Angles Postulate.** If two lines cut by a transversal form congruent corresponding angles, then the lines are parallel.

**Converse of the Alternate Interior Angles Theorem.** If two lines cut by a transversal form congruent alternate interior angles, then the lines are parallel.

**Converse of the Alternate Exterior Angles Theorem.** If two lines cut by a transversal form congruent alternate exterior angles, then the lines are parallel.

**Converse of the Co-Interior Angles Theorem.** If two lines cut by a transversal form supplementary co-interior angles, then the lines are parallel.

So now you have a **two-way street** for each angle pair:
- "Lines parallel ⟹ corresponding angles congruent" (Lesson 3)
- "Corresponding angles congruent ⟹ lines parallel" (this lesson)

Together they form a biconditional: *two lines cut by a transversal are parallel **if and only if** corresponding angles are congruent.*

**Two more facts** (small but useful):

- **Two lines perpendicular to the same line are parallel.** You met this in Lesson 1; now you can also see it as a corollary of the converse theorems (the right angles at each crossing are corresponding angles).
- **Two lines parallel to the same line are parallel to each other.** Transitivity of parallel.

**Recall the logic** from Unit 1: a statement and its converse are *not* automatically equivalent. The fact that all four converses here turn out to be true is a genuine theorem — they didn't have to be. But because they are, we can use any of the angle relationships to *conclude* parallel, not just to *deduce* angle measures from parallel.

---

## Manipulate (interactive)

Two lines on the canvas — initially not parallel — with a transversal crossing them. The bottom line pivots around a fixed point and is draggable. The canvas tracks one corresponding angle pair live.

**Drag the bottom line until the corresponding angle pair becomes congruent.** At that moment, the lines snap into parallel and matching arrows appear on both lines. Success.

This is the converse direction in your hands: you're using the angle relationship to *make* the lines parallel.

*Why this matters:* in a proof, you often need to *show* lines are parallel from given angle information. This is exactly the move.

---

## Worked Example

> Lines $a$ and $b$ are cut by transversal $t$. ∠3 (interior, lower-right at top intersection) and ∠5 (interior, upper-left at bottom intersection) are an alternate interior pair. ∠3 = 72°, ∠5 = 72°. Are $a$ and $b$ parallel?

**Step 1.** Identify the relationship in the given information. ∠3 and ∠5 are alternate interior angles, and they're congruent (both 72°).

**Step 2.** Choose the right theorem. The Converse of the Alternate Interior Angles Theorem says: *if alternate interior angles are congruent, then the lines are parallel.*

**Step 3.** Apply it. ∠3 ≅ ∠5 (given), and they're alternate interior, so $a \parallel b$.

**Answer:** Yes, $a \parallel b$ by the Converse of the Alternate Interior Angles Theorem.

Notice that we didn't need to find any other angles — we just used the given pair and the right theorem. In proofs, that's the move: identify the pair, name the converse, conclude parallel.

---

## Reflection

Every parallel-line theorem from Lesson 3 has a converse that's also true. Together they form a biconditional: an angle pair is congruent (or supplementary, for co-interior) **iff** the lines are parallel.

When you're given angle information and asked whether lines are parallel — or asked to prove they are — name the angle pair, cite the matching converse theorem, and you're done.

Next up: parallel and perpendicular in the coordinate plane. The slope is about to do all the work the angle pairs did in this unit.
