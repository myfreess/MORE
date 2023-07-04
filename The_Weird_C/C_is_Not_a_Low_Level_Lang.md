# C不是一种低级语言

## *你的计算机也不是仅仅快一点的PDP-11!*

**作者: DAVID CHISNALL**

这篇文章除去没什么内容的第一节*WHAT IS A LOW-LEVEL LANGUAGE?*, 余下的内容基本可以分为两部分，分别对应主标题和副标题

+ C Is Not a Low-level Language - 低级语言的关键特性之一是语言的抽象机器模型可以较轻松地对应到物理机器，工业级C编译器复杂的优化和在一些语言细节上语焉不详的ISO C标准使得C语言成功脱离低级语言之列。
+ Your computer is not a fast PDP-11 - C的顺序执行模型导致现代硬件不得不在迁就C语言的情况下束手束脚地进行并行优化，畸形膨胀的优化措施最终导致了Meltdown和Spectre这样的致命漏洞。

> 录入者注：某位arch人提醒我，任何涉及fast/slow path的场合都存在类似的问题

DeepL机翻

## 序言

> In the wake of the recent Meltdown and Spectre vulnerabilities, it’s worth spending some time looking at root causes. Both of these vulnerabilities involved processors speculatively executing instructions past some kind of access check and allowing the attacker to observe the results via a side channel. The features that led to these vulnerabilities, along with several others, were added to let C programmers continue to believe they were programming in a low-level language, when this hasn’t been the case for decades.

在最近发生的Meltdown和Spectre漏洞之后，值得花一些时间来研究其根源。这两个漏洞都涉及到处理器试探性地执行指令，越过某种访问检查，并允许攻击者通过旁路观察其结果。导致这些漏洞的功能，以及其他一些功能，是为了让C语言程序员继续相信他们是在用低级语言编程，而几十年来，情况并非如此。

> Processor vendors are not alone in this. Those of us working on C/C++ compilers have also participated.

不仅仅是处理器厂商, 我们这些从事C/C++编译器的人也涉足其中。

## WHAT IS A LOW-LEVEL LANGUAGE?

> Computer science pioneer Alan Perlis defined low-level
languages this way:

计算机科学先驱Alan Perlis这样定义低级语言:



## FAST PDP-11 EMULATORS

## OPTIMIZING C

## UNDERSTANDING C

## IMAGINING A NON-C PROCESSOR