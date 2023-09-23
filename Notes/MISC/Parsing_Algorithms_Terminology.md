# A Guide to Parsing Algorithms and Terminology - part 3

## Overview

parsing是一个已经解决的问题，并且还可以被解决许多次。

### Two Strategies

+ top-down方法从根节点开始创建语法树
+ bottom-up方法从叶节点开始创建语法树

传统上认为top-down更好写，bottom-up更强力

### Common Elements

top-down和bottom-up parser有一些共同的地方

#### Lookahead and Backtracking

lookahead只偷看后续的n个token，但是不改变当前位置


#### Chart Parser

使用动态规划代替回溯的一类Parser,如CYK, Earley.

### Automatons

PDA啦

## Tables of Parsing Algorithms

原文提供了一份有大量引用资料的表格，以及各种parsing算法的特点与应用场景。

## Top-down Algorithms

应用非常广泛

### LL Parser

LL(**L**eft-to-right read of the input, **L**eftmost derivation) parser是一类表驱动的无回溯parser，它可以处理LL语法

LL(k)即lookahead k个token的parser/LL(k)可以处理的语法(LL(1)最好实现)。后者的概念使用更广，如PEG Parser可以处理LL(*)语法(`*`指可变数量)。

#### The Value of LL Grammer

LL语法不支持左递归，尽管任何存在左递归的语法都可以转化为等价的无左递归形式，但限制仍存在于两方面：productivity(需要花时间重写语法)和power(本来只需要lookahead一个token的语法改写后可能需要lookahead较多token)

前者可以由自动化工具解决，如ANTLR.

许多语言的语法是LL(1)加上一点点含不确定性的特例。

### Earley Parser

Jay Earley发明的一种chart parser。

它适用于所有CFG，大多数时候具有线性时间复杂度。

很适合给新语言做高亮这种要求开发速度快的任务。

### Packrat (PEG)

Packrat parser常常与它所对应的语法PEG一同被提及，它们都是Bryan Ford发明的。

也不支持左递归。有些变种可以，那就做不到线性时间了。

### Recursive Descent Parser

一种由多个互递归过程组合而成的parser。

predictive parser有时就是top-down parser，有时指没有回溯的Recursive Descent Parser

### Pratt Parser

直接工作在token上，不太关心语法的parser，专精处理各种表达式。

发明人Vaughan Pratt.

据说用的人不多，但用过的普遍表示非常感谢Pratt。

rust analyzer和JSLint都使用了它。

### Parser Combinator

不说

## Bottom-up Algorithms

Bottom-up 策略的旗舰产品是LR Parser。

更加宽泛的一种bottom-up parser是shift-reduce parser，它包含了LR parser

### CYK Parser

需要语法以乔姆斯基范式(Chomsky normal form)表达

### LR Parser

程序生成的LR Parser可以在线性时间内处理确定性上下文无关语言，无需回溯