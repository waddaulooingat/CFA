## concept

The **Angle-Angle-Side (AAS) Congruence Theorem** states: *If two angles and a non-included side of one triangle are congruent to two angles and the corresponding non-included side of another triangle, then the triangles are congruent.* The key word here is "non-included" — unlike ASA, the known side does NOT sit between the two known angles.

AAS is a **theorem**, not a postulate, meaning it can be *proved* from other results rather than assumed as an axiom. The proof is elegant: if you know two angles of a triangle (say ∠A and ∠B), the third angle ∠C is completely determined because ∠A + ∠B + ∠C = 180°. Therefore, knowing ∠A and ∠B automatically gives you ∠C as well — you secretly know all three angles. Now with ∠A, ∠C, and side AC (the side between them), you have ASA. So AAS is really ASA in disguise; the Triangle Angle Sum Theorem does the bridging work.

To distinguish AAS from ASA, carefully check whether the given side is between the two given angles. In △ABC, if you know ∠A and ∠B, the included side is AB (since it connects A and B). If instead the given side is BC (which connects B and C, but ∠C is not one of the given angles), then BC is non-included — you have AAS. A helpful rule: list the two angles and the side in order around the triangle. If the side comes last or first in the listing (e.g., Angle-Angle-Side or Side-Angle-Angle), it is non-included (AAS). If the side is in the middle (Angle-Side-Angle), it is included (ASA).

AAS appears frequently when two parallel lines are cut by a transversal. The transversal creates two triangles sharing a common side (the transversal segment between the parallel lines). Alternate interior angles give one pair of congruent angles; a second angle pair can come from vertical angles or another pair of alternate interior angles. Since the shared transversal side is typically opposite one of those angles (not between both), the configuration is AAS.

A practical example: in the triangles formed by the diagonals of a kite, you can identify two angle pairs using angle bisector properties and vertical angles. The known side is often the diagonal itself, which is opposite (not between) the given angle pair. Recognizing this as AAS — rather than trying to force it into ASA — is an important skill in proof-writing.

---

## reflection

AAS completes the set of congruence tools for non-right triangles: SSS, SAS, ASA, and AAS. Together, these four cover every minimal set of measurements that uniquely determines a triangle (subject to the "included" constraints). The fact that SSA is NOT in this list is a reminder that not every combination of three measurements is sufficient — a lesson in mathematical precision.

The relationship between AAS and ASA illustrates how theorems build on each other. ASA is accepted as a postulate; AAS is derived from it using the Triangle Angle Sum Theorem. This chain — postulate → theorem → new theorem — is the engine of deductive geometry. In later courses, you will see this pattern again: key axioms form a foundation, and increasingly powerful theorems are derived on top of them.

AAS becomes especially useful in coordinate geometry proofs and in proving properties of special quadrilaterals. When you need to show that a parallelogram has congruent diagonals, or that the base angles of an isosceles triangle are equal, you will frequently reach for AAS or ASA as the final step before invoking CPCTC. Mastering the distinction between these two theorems now pays dividends throughout the rest of the course.
