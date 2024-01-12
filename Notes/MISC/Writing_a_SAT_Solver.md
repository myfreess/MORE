# 写个SAT Solver

先写个概括提纲：

1. SAT问题(Boolean satisfiability problem)是一类这样的问题：有N个非此即彼的条件，可以表示为布尔变量，然后有一些针对条件的约束，要找一组赋值方式满足所有约束。
2. 这个问题首先一定能解决，变量数量有限(假设N个)，穷举只不过2^N种可能，这也是最简单的解法。
3. 这种解法太过低效，但是令人惊奇的是，关于求解SAT问题没有渐进复杂度更好的算法！因为SAT问题的一些子集(如3-SAT)被证明是所谓的“NP完全问题”(Non-deterministic Polynomial Complete Problem,有时也叫NPC问题)。

> 粗略一点讲，NP问题是这样的一类问题：如果给出答案，可以在多项式时间复杂度内验证答案对不对，但是不一定能在多项式时间内完成求解。而NP完全问题是NP问题的一个拥有更强性质的子集：如果找到某个NP完全问题的多项式解法，那么所有NP问题都可以在多项式时间内完成求解。

3. 尽管如此，还是能够从工程角度给出一些启发式的求解算法，其中著名而经典的一种是DPLL算法。该算法基于布尔表达式的一种特殊形式：合取范式(Conjunctive Normal Form, CNF)
4. 最后总结一些在DPLL上的微操，还有基于DPLL和子句学习(clause learning)的冲突驱动子句学习(Conflict Driven Clause Learning, CDCL)算法.

主要参考资料：https://andrew.gibiansky.com/blog/verification/writing-a-sat-solver/

知乎用户AlephAlpha翻译了一篇关于CDCL算法的教程，其原文还提供了一些交互式的小例子，故一并列出：

https://zhuanlan.zhihu.com/p/92659252

https://cse442-17f.github.io/Conflict-Driven-Clause-Learning/

但是文字描述真的太少了，看不懂，Richard Tichy, Thomas Glase写的Clause Learning in SAT个人感觉好懂得多

该文中还贴心地发了点实现SAT solver的参考资料：

http://minisat.se/MiniSat.html

另外这个也不错: https://codingnest.com/modern-sat-solvers-fast-neat-and-underused-part-3-of-n/

## 例子：购物问题

## 理论部分

### CNF

### Tseitin编码

### DPLL

### CDCL

