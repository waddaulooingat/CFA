# Skill 2 — Segments and Rays

**Skill ID:** `geo-u1-segments-and-rays`
**Difficulty band:** 1
**Prereqs:** geo-u1-point-line-plane

---

## Concept

A line keeps going forever. A **segment** and a **ray** are pieces of a line — but they're cut off in different ways.

A **segment** $\overline{AB}$ is the part of a line that starts at *A*, ends at *B*, and includes everything between. Notation: bar over the letters, $\overline{AB}$. Length is denoted $AB$ with no bar — that's a number. So $\overline{AB}$ is the *segment* (a geometric object) and $AB$ is its *length* (a number). Mixing them up is the most common notation error in geometry.

A **ray** $\overrightarrow{AB}$ starts at *A*, passes through *B*, and continues forever past *B*. The endpoint is always the first letter. $\overrightarrow{AB}$ and $\overrightarrow{BA}$ are **different rays** — they start at different endpoints and point opposite directions.

Two facts you'll use constantly:

**Segment Addition Postulate.** If point *B* lies on segment $\overline{AC}$ (between *A* and *C*), then $AB + BC = AC$.

**Midpoint.** Point *M* is the midpoint of $\overline{AB}$ if *M* lies on $\overline{AB}$ and $AM = MB$. The midpoint cuts a segment into two equal halves.

**Congruent segments.** Two segments are **congruent** if they have the same length. Notation: $\overline{AB} \cong \overline{CD}$ means the *segments* are congruent. $AB = CD$ means the *lengths* are equal. Both say the same thing, just at different levels of formality.

---

## Manipulate (interactive)

You'll see segment $\overline{AB}$ with point *M* on it. Point *M* is draggable. **Drag *M* to the midpoint of $\overline{AB}$.**

The canvas will show *AM* and *MB* live as you drag. When *AM = MB* to within rounding, the success message appears.

*Why this matters:* every triangle midsegment, every two-column proof involving midpoints, every coordinate-geometry midpoint formula starts with this picture in your head.

---

## Worked Example

> Point *B* lies on segment $\overline{AC}$. *AB* = 7 and *BC* = 12. Find *AC*.

**Step 1.** Read the picture. *B* is between *A* and *C*, so the Segment Addition Postulate applies.

**Step 2.** Write the postulate with our values: $AB + BC = AC$, which gives $7 + 12 = AC$.

**Step 3.** Add: $AC = 19$.

**Step 4.** Sanity check. *AC* should be larger than both *AB* and *BC* (since both fit inside it). 19 > 12 and 19 > 7. Good.

**Answer:** $AC = 19$.

A common trap: don't confuse this with "find the longer piece." The whole segment *AC* is what you want.

---

## Reflection

Three shapes, three notations. Line goes on forever both ways. Ray has one endpoint and goes forever the other way. Segment is finite, with two endpoints.

The two postulates from this lesson — Segment Addition and the definition of midpoint — will show up again and again. When you see "B is between A and C," your hand should reach for $AB + BC = AC$ automatically.

Next up: angles. Same idea (a piece of something larger), different shape.
