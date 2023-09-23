# From PwD to PwZ

Consider the classic context-free grammar, the language of balanced parens:
S = S ( S ) ∪ ε

Consider the derivative with respect to an open paren:
[D( S] = [D( S] ( S ) ∪ [S )]

It's infinitely recursive.

Fine for math. Bad for implementation.

Fortunately, it's not hard to stop this infinite recursion.

If we compute lazily, then the derivative terminates.

But, because the grammar is left-recursive, taking another derivative will force the computation when it tries to check whether the derived language contains the empty string.

So, that nullability check causes non-termination.

But, that's easy to fix too: just memoize the derivative.

In fact, the hardest part about computing the derivative is figuring out whether or not a language contains the empty string. The definition for nullability, δ(L), is also structurally recursive:

    δ(∅) = false
    δ(ε) = true
    δ(c) = false
    δ(A ○ B) = δ(A) and δ(B)
    δ(A ∪ B) = δ(A) or δ(B).

Laziness and memoization don't work here.

Instead, the function δ has to be computed as a least fixed point.

we assume all nodes are not nullable, and recompute the nullability of all nodes until no changes occur.



