## concept

**CPCTC** stands for **Corresponding Parts of Congruent Triangles are Congruent**. It is not a postulate about establishing congruence — it is a conclusion drawn *after* two triangles have already been proven congruent. CPCTC answers the question: "Once I know △ABC ≅ △DEF, what else do I know?" The answer: every pair of corresponding parts (sides and angles) is congruent.

The key word is "corresponding." When you write △ABC ≅ △DEF, you are declaring a specific vertex-to-vertex pairing: A↔D, B↔E, C↔F. From this pairing, six correspondences follow automatically: three pairs of sides (AB↔DE, BC↔EF, CA↔FD) and three pairs of angles (∠A↔∠D, ∠B↔∠E, ∠C↔∠F). CPCTC says every one of these six pairs is congruent. You only needed a subset of them to *prove* the triangles congruent (e.g., three pairs for SSS, or two sides and the included angle for SAS); CPCTC gives you the rest for free.

CPCTC appears in the second half of a two-part proof structure. Part one: establish that two triangles are congruent using a congruence postulate or theorem (SSS, SAS, ASA, AAS, or HL). Part two: invoke CPCTC to extract the specific pair of parts that the proof needs to conclude. For example, a proof might need to show that segment BD bisects angle ∠ABC. The strategy: prove △ABD ≅ △CBD by SAS, then conclude ∠ABD ≅ ∠CBD by CPCTC, and finally cite the definition of an angle bisector.

A common error is applying CPCTC before the triangle congruence step — using it as a reason to establish that parts are equal in order to later conclude the triangles are congruent. This is circular reasoning. The logical order must always be: (1) use given information to show enough conditions for a congruence postulate; (2) state the triangle congruence; (3) invoke CPCTC for the desired parts. CPCTC always comes last among the geometric steps.

CPCTC is powerful precisely because it unlocks *all* corresponding parts simultaneously. Once △ABC ≅ △DEF is established, you may use CPCTC to conclude any or all of the six corresponding pairs in subsequent steps of the same proof. This makes it especially valuable in multi-part proofs where you need several conclusions from one triangle congruence.

---

## reflection

CPCTC is the payoff of all the congruence work in this unit. Every time you prove two triangles congruent — using SSS, SAS, ASA, AAS, or HL — you unlock CPCTC as a tool. This two-step pattern (prove congruence, then extract parts) is one of the most frequently used structures in all of Euclidean geometry. Recognizing when a proof is "heading toward CPCTC" is a hallmark of geometric maturity.

CPCTC connects to real-world reasoning wherever you need to transfer a property from one object to another via congruence. In engineering, two identical (congruent) parts have all the same dimensions — CPCTC is the formal statement of this intuition. In art and design, symmetric figures contain congruent triangles, and CPCTC explains why corresponding measurements are equal across a line of symmetry. In computer graphics, transformation matrices preserve triangle congruence, and CPCTC justifies why shapes look identical after a rigid motion.

Looking ahead, CPCTC will be your most-used reason in the two-column proofs of Lesson 7. You will also encounter it in proofs about quadrilaterals (parallelograms, rhombuses, rectangles) and circles. The habit to build now: whenever you see "prove that two segments are equal" or "prove that two angles are equal," think immediately about which triangles contain those parts and how to prove those triangles congruent.
