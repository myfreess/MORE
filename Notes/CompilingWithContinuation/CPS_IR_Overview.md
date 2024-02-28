Although the mathematical expression (a + 1) ∗ (3 + c) makes no assump-
tion about whether a + 1 or 3 + c is to be evaluated first, any translation into
continuation-passing style must choose one or the other. In the example above,
a + 1 is to be evaluated first. This is another essential feature of CPS: many deci-
sions about control flow are made during the conversion from the source language
into CPS. These decisions are not irreversible, however—an optimizer could, after
some analysis, determine that the continuation expression could be rearranged to
evaluate 3 + c first.

虽然数学表达式 (a + 1) ∗ (3 + c) 并没有假设是先求值 a + 1 还是先求值 3 + c，但如果要翻译成续延传递方式，则必须二选一。在上面的例子中，a + 1 应先求值。这是 CPS 的另一个基本特征：在从源语言转换为 CPS 的过程中，要做出许多关于控制流的决定。然而，这些决定并不是不可逆转的--优化器经过分析后，可以重新安排续延表达式，让 3 + c 先求值。
