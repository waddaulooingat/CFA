# Skill 3 — Parallel Line Theorems

**Skill ID:** `geo-u2-parallel-theorems`
**Difficulty band:** 1
**Prereqs:** geo-u2-transversal-angles

---

## Concept

When the two lines crossed by a transversal are **parallel**, the named pairs from the last lesson stop being just names — they obey strict angle relationships.

**Corresponding Angles Postulate.** If two parallel lines are cut by a transversal, then **corresponding angles are congruent**.

This one is a postulate — accepted without proof. Every other theorem on this page is built on it.

**Alternate Interior Angles Theorem.** If two parallel lines are cut by a transversal, then **alternate interior angles are congruent**.

**Alternate Exterior Angles Theorem.** If two parallel lines are cut by a transversal, then **alternate exterior angles are congruent**.

**Co-Interior Angles Theorem** (also called Same-Side Interior Angles or Consecutive Interior Angles). If two parallel lines are cut by a transversal, then **co-interior angles are supplementary** (sum to 180°).

The pattern:
- **Corresponding, alt interior, alt exterior** → *congruent*
- **Co-interior** → *supplementary*

Three congruent, one supplementary. The odd one out is the only pair on the *same* side of the transversal.

**Why it works (intuition, not a proof).** Slide one parallel line along the transversal to lie directly on top of the other. Every angle at one intersection lands on the angle in the same position at the other. So same-position pairs (corresponding) match exactly. The other relationships fall out from vertical angles and linear pairs once corresponding is settled.

---

## Manipulate (interactive)

You'll see two parallel lines cut by a transversal. The transversal is draggable — its angle of crossing changes as you drag. All eight angles' measurements display live.

**Drag the transversal** through several positions and watch what happens:

- All four corresponding pairs stay equal.
- Both alt interior pairs stay equal.
- Both co-interior pairs always sum to 180°.

When you've observed the pattern through three different transversal positions, the success message fires. There's no right answer here — it's confirmation. You're convincing your gut that the theorems hold no matter the angle.

*Why this matters:* you'll use these theorems in dozens of proofs. The feel for "yes, they really do always match" is what makes those proofs fast.

---

## Worked Example

> Lines $\ell$ and $m$ are parallel, cut by a transversal. ∠1 measures 65°. Find the measures of ∠2 through ∠8. (Standard numbering: ∠1–∠4 clockwise at top intersection from upper-left; ∠5–∠8 same way at bottom.)

**Step 1.** Use linear pair and vertical angles at the top intersection.
- ∠1 = 65°.
- ∠2 forms a linear pair with ∠1, so ∠2 = 180° − 65° = **115°**.
- ∠3 is vertical to ∠1, so ∠3 = **65°**.
- ∠4 is vertical to ∠2, so ∠4 = **115°**.

**Step 2.** Use the Corresponding Angles Postulate to fill in the bottom intersection.
- ∠5 corresponds to ∠1, so ∠5 = **65°**.
- ∠6 corresponds to ∠2, so ∠6 = **115°**.
- ∠7 corresponds to ∠3, so ∠7 = **65°**.
- ∠8 corresponds to ∠4, so ∠8 = **115°**.

**Step 3.** Sanity check with the other theorems.
- Alternate interior ∠3 and ∠5: both 65°. ✓
- Co-interior ∠4 and ∠5: 115° + 65° = 180°. ✓

**Pattern to internalize.** With parallel lines and one angle known, every other angle is either equal to it or supplementary to it. Half the angles are one measure; the other half are the supplement. That's the whole story.

---

## Reflection

Four theorems. Three say "congruent" (corresponding, alt interior, alt exterior), one says "supplementary" (co-interior). The supplementary pair is the only one on the same side of the transversal.

Once you know one angle, you know all eight. They split into two groups: those equal to your known angle, and those supplementary to it.

Next up: the converses. If we don't know whether the lines are parallel but we see one of these angle relationships, can we *conclude* the lines are parallel? Spoiler: yes.
