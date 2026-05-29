# Skill 5 — Counterexamples

**Skill ID:** `geo-u1-counterexamples`
**Difficulty band:** 1
**Prereqs:** geo-u1-if-then-logic

---

## Concept

You only need **one** example to disprove a "for all" statement. That example is called a **counterexample**.

A counterexample to a conditional $p \rightarrow q$ is a single case where *p* is true but *q* is false. One counterexample is enough — you don't need a pattern or many examples. The whole conditional crumbles with a single bad case.

This is asymmetric: to **prove** "all X are Y" you generally need a proof; to **disprove** it, one counterexample is enough.

**Example.** Statement: *"All prime numbers are odd."*
Counterexample: 2. It's prime, and it's even. One number does the job; we don't need any others. The statement is false.

**Example.** Statement: *"If two angles are congruent, then they are vertical angles."*
Counterexample: two right angles in opposite corners of a room. Both measure 90°, so they're congruent, but they aren't formed by intersecting lines so they aren't vertical. Done.

Three habits that make finding counterexamples faster:

1. **Test the edges.** Zero, one, negative numbers, special triangles (right, equilateral, degenerate). Statements often fail at boundary cases.
2. **Read carefully.** Look for words like "all," "every," "always" — those are the words you're attacking. Words like "some" or "there exists" cannot be disproved by a single example.
3. **Don't overthink.** A counterexample should be the simplest case you can find. If you're constructing something complicated, you've usually missed a simple one.

A statement that resists every counterexample is a candidate for a real proof. That's how mathematicians decide where to spend effort.

---

## Manipulate (interactive)

Statement on screen: *"If a triangle has two equal sides, then it has a right angle."*

The canvas shows a triangle with one draggable vertex and live measurements. **Drag the vertex until the triangle is a counterexample** — meaning two sides are equal, AND no angle is 90°.

When you've built one, the statement on screen turns red and a "Disproved!" banner appears. Aim to do it in under 30 seconds.

*Why this matters:* counterexamples are the way you'll personally check whether a "rule" your gut wants to apply is actually a rule. Save yourself wrong answers in proofs by checking with one before you commit.

---

## Worked Example

> Statement: *"If a quadrilateral has four equal sides, then it is a square."* Find a counterexample.

**Step 1.** Read carefully. The hypothesis is "four equal sides"; the conclusion is "is a square."

**Step 2.** I need a quadrilateral with four equal sides that isn't a square. What four-sided shapes have all sides equal?
- Square — all sides equal AND all angles 90°.
- Rhombus — all sides equal, but angles need not be 90°.

**Step 3.** A non-square rhombus (one with no right angles) has four equal sides but isn't a square.

**Step 4.** Counterexample: *a rhombus with angles of 60° and 120°.* All four sides are equal; it isn't a square.

**Answer:** A non-square rhombus is a counterexample. The statement is false.

A small trap: the counterexample must satisfy the **hypothesis**, not just be unusual. A triangle wouldn't work here even though it isn't a square, because it doesn't have four equal sides — so the hypothesis already fails and the conditional says nothing about it.

---

## Reflection

One bad case is enough to kill a "for all" statement. To **find** that bad case, attack the edges, read for "all/every/always," and try the simplest thing first.

A counterexample must satisfy the *hypothesis* but fail the *conclusion*. Skip the hypothesis check at your peril — that's the most common mistake here.

This wraps Unit 1. With foundations and logic in hand, Unit 2 starts the real geometry: parallel lines cut by a transversal, and the eight angles that form.
