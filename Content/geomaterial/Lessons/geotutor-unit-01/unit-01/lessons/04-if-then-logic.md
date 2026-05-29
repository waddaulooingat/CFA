# Skill 4 — If-Then Logic

**Skill ID:** `geo-u1-if-then-logic`
**Difficulty band:** 1
**Prereqs:** geo-u1-point-line-plane

---

## Concept

Every theorem in geometry is a sentence in the form *if X, then Y*. These are called **conditional statements**.

In *"If it is raining, then the ground is wet,"* the part after **if** is the **hypothesis** (*p*) and the part after **then** is the **conclusion** (*q*). We write the conditional as $p \rightarrow q$.

A conditional is **true** when the hypothesis genuinely forces the conclusion. It's **false** when you can find a single case where the hypothesis holds but the conclusion fails — that case is called a counterexample (the next skill).

From any conditional $p \rightarrow q$ you can build three related statements:

| Name | Form | Reads as |
|---|---|---|
| Conditional | $p \rightarrow q$ | If *p*, then *q*. |
| **Converse** | $q \rightarrow p$ | If *q*, then *p*. (Swap hypothesis and conclusion.) |
| **Inverse** | $\sim p \rightarrow \sim q$ | If not *p*, then not *q*. (Negate both.) |
| **Contrapositive** | $\sim q \rightarrow \sim p$ | If not *q*, then not *p*. (Swap *and* negate.) |

Two facts worth memorizing right now:

1. A conditional and its **contrapositive** always have the same truth value. If one is true, so is the other.
2. A conditional and its **converse** are independent. Either can be true while the other is false. *"If a figure is a square, then it has four sides"* is true; the converse *"If a figure has four sides, then it is a square"* is false (a rectangle has four sides and isn't a square).

**Biconditional.** When both $p \rightarrow q$ and $q \rightarrow p$ are true, we write $p \leftrightarrow q$ and read it *"p if and only if q"* — abbreviated *iff*. Most geometric definitions are biconditional: a triangle is equilateral iff all three sides are congruent. The "iff" means the definition works in both directions.

---

## Manipulate (interactive)

You'll see four statements about a triangle on the canvas, with a draggable vertex. **Drag the vertex** to make the triangle change shape and watch which statements light up green (true) or red (false) as it changes.

This isn't a "find the right answer" task — it's a feel-for-it exercise. By the end of a minute of dragging you should see that "all sides equal" and "all angles equal" are *linked* (one forces the other), but "has a right angle" is independent of both.

*Why this matters:* feeling the linkage between conditions is the muscle memory behind every proof.

---

## Worked Example

> Statement: *"If a polygon is a square, then it has four sides."* Write the converse, inverse, and contrapositive. Decide whether each is true or false.

**Step 1.** Identify the parts.
- Hypothesis *p*: "a polygon is a square"
- Conclusion *q*: "it has four sides"

**Step 2.** Build the three derivatives.
- **Converse** $q \rightarrow p$: "If a polygon has four sides, then it is a square."
- **Inverse** $\sim p \rightarrow \sim q$: "If a polygon is not a square, then it does not have four sides."
- **Contrapositive** $\sim q \rightarrow \sim p$: "If a polygon does not have four sides, then it is not a square."

**Step 3.** Check truth values.
- Original: **True**. Squares have four sides by definition.
- Converse: **False**. A rectangle has four sides and isn't a square. (Rectangle is the counterexample.)
- Inverse: **False**. A rectangle is not a square but does have four sides.
- Contrapositive: **True**. Anything without four sides cannot be a square — squares require four sides.

Notice that the original and the contrapositive match (both true), and the converse and inverse match (both false). That's the pattern: they pair up.

---

## Reflection

A conditional says *if p, then q*. From it you can build three more statements: converse (swap), inverse (negate), contrapositive (swap *and* negate).

The single most useful fact in this skill: **a statement and its contrapositive are logically equivalent**. When you can't prove a statement directly, you can sometimes prove its contrapositive instead — that's a major proof technique you'll see in Unit 13.

Next up: counterexamples — the smallest unit of disproof in geometry.
