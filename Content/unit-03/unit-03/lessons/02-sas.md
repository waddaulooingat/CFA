## concept

The **Side-Angle-Side (SAS) Congruence Postulate** states: *If two sides and the included angle of one triangle are congruent to two sides and the included angle of another triangle, then the triangles are congruent.* The word "included" is critical — the angle must be the one *formed by* those two sides, sitting between them at their shared vertex.

To understand why "included" matters, consider what happens with SSA (two sides and a *non*-included angle). Given sides AB = DE = 5, BC = EF = 4, and ∠C = ∠F = 30°, two entirely different triangles can satisfy these conditions — the angle at C is opposite AB, and the triangle can "swing" into two valid configurations. This ambiguity (the "ambiguous case") is why SSA is not a valid congruence postulate. SAS avoids this problem by locking the angle between the two sides, eliminating any ambiguity.

To verify an included angle, trace the two sides to their shared vertex. In △ABC, if you are given AB and BC, the included angle is ∠B — the angle at the vertex shared by both segments. In notation, SAS for △ABC ≅ △DEF might read: AB ≅ DE (side), ∠B ≅ ∠E (included angle), BC ≅ EF (side). The congruent angle sits in the middle of the listing, symbolizing its position between the two sides.

SAS appears frequently with the **Reflexive Property**: when two triangles share a common side, that shared side is one of the two "S" pairs. Combined with one more congruent side and the included angle, SAS applies. It also arises with **bisectors**: an angle bisector splits an angle into two equal parts, and if two triangles on either side share the bisecting segment, you have two sides (the segment and a matching pair) plus the bisected angle — a natural SAS setup.

A coordinate geometry check for SAS: compute the two side lengths using the distance formula, then verify the included angle using the dot product or the law of cosines. For a triangle with B at the origin, A at (0,5), and C at (4,0), the angle at B can be found from cos(∠B) = (BA⃗ · BC⃗)/(|BA||BC|) = (0·4 + 5·0)/(5·4) = 0, so ∠B = 90°. If a second triangle has the same two side lengths and a 90° included angle, SAS confirms congruence.

---

## reflection

SAS is the second pillar of triangle congruence, and its "included" requirement teaches a deeper lesson: in geometry, the *relationship* between pieces of information matters, not just the pieces themselves. Two triangles with two congruent sides and one congruent angle can fail to be congruent if the angle is in the wrong position — a nuance that sharpens careful, precise reasoning.

SAS connects directly to the Law of Cosines from trigonometry: c² = a² + b² − 2ab·cos(C). Given two sides a, b and the included angle C, the formula produces a unique third side c. That uniqueness is precisely why SAS guarantees congruence — the two given sides plus the included angle completely determine the triangle. When you study trigonometry, you will use SAS (or its trig analogue) to solve triangles in real contexts, from navigation to physics.

Coming up next, ASA and AAS will show that angle information can substitute for side information in establishing congruence. As you progress through these postulates, notice a pattern: each one represents a minimal set of measurements that locks down a triangle uniquely. The art of proof is identifying which of these minimal sets is present in the given information.
