> 原文：https://andrew.gibiansky.com/blog/verification/writing-a-sat-solver/

> 作者：Andrew Gibiansky :: Math -> [Code]

先写个概括提纲：

0. SAT问题(Boolean satisfiability problem)是一类这样的问题：有N个非此即彼的条件，可以表示为布尔变量，然后有一些针对条件的约束，要找一组赋值方式满足所有约束。
1. 这个问题首先一定能解决，变量数量有限(假设N个)，穷举只不过2^N种可能，这也是最简单的解法。
2. 这种解法太过低效，但是令人惊奇的是，关于求解SAT问题没有渐进复杂度更好的算法！因为SAT问题的一些子集(如3-SAT)被证明是所谓的“NP完全问题”(Non-deterministic Polynomial Complete Problem,有时也叫NPC问题)。

> 粗略一点讲，NP问题是这样的一类问题：如果给出解答，可以在多项式时间复杂度内验证答案对不对，但是不一定能在多项式时间内完成求解。而NP完全问题是NP问题的一个拥有更强性质的子集：如果找到某个NP完全问题的多项式解法，那么所有NP问题都可以在多项式时间内完成求解。详细而严谨的解释恐怕要涉及一些数学和复杂性理论，我不会啊。

3. 尽管如此，还是能够从工程角度给出一些启发式的求解算法，Andrew选择了其中著名而经典的一种：DPLL算法。该算法基于布尔表达式的一种特殊形式：合取范式(Conjunctive Normal Form, CNF)
4. 最后总结一些在DPLL上的微操，还有基于DPLL和子句学习(clause learning)的冲突驱动子句学习(Conflict Driven Clause Learning, CDCL)算法.

顺便一提，知乎用户AlephAlpha翻译了一篇关于CDCL算法的教程，其原文还提供了一些交互式的小例子，故一并列出：

https://zhuanlan.zhihu.com/p/92659252

https://cse442-17f.github.io/Conflict-Driven-Clause-Learning/

该文中还贴心地发了点实现SAT solver的参考资料：

http://minisat.se/MiniSat.html

我试图用clojure写一遍(当然是直接写DPLL)，一遍练习不太熟悉的clojure，一边了解SAT solver相关的基本知识，不过考虑到是clojure，大概会慢到离谱吧。

一动手发现摆在我面前的问题还不少。

## 抽象语法树和动态类型

## 穷人的Alternative
