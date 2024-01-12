## 12.1 简介

On a typical commodity microprocessor, integer
addition and subtraction require fewer cycles than integer division; simi-
larly, floating-point division takes more cycles than floating-point addition
or subtraction. Multiplication usually falls between the corresponding addi-
tion and division operations. The cost of a load from memory depends on
where in the memory hierarchy the loaded value resides at the time that the
load issues.

Scheduling for single basic blocks is NP-complete under almost any realis-
tic scenario.

The dominant technique for instruction scheduling is a greedy heuristic
called list scheduling. List schedulers operate on straight-line code and use
a variety of priority ranking schemes to guide their choices.

To speed up program execution, processor architects added features such as
pipelined execution, multiple functional units, and out-of-order execution

对指令重新排序，有时候为了消除指令间的假依赖(antidependences)，还得多用几个空闲寄存器

## 12.2 背景

