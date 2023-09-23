> 笔记性质, 原文为Pavel Panchekha的4篇系列博客，作于2011年左右

```
Part1 : https://pavpanchekha.com/blog/zippers/huet.html
Part2 ：https://pavpanchekha.com/blog/zippers/derivative.html
Part3 ：https://pavpanchekha.com/blog/zippers/kiselyov.html
Part4 ：https://pavpanchekha.com/blog/zippers/multi-zippers.html
```

顺便一提现在作者在招phd，他是是应用程序语言理论方面的研究者，主要兴趣是浏览器(有点迷)，有意快看看

https://pavpanchekha.com/hiring.html

Zipper是函数式编程中一种加速数据结构修改的技术，作为一种在函数式社区中流传了有些年头的设计模式，它在1997年被Huet`[1]`正式提出并命名，其后Conor McBride`[2]`提出此模式实际上与微分运算大有关联，Oleg Kiselyov`[3]`基于微分这一思想实现了更为强大的Zipper，而Pavel Panchekha在MIT 6.854 Advanced Algorithms课程上的结课论文`[4]`中提出了一种快速在多个点更新数据的MultiZipper方法。

`[1]`：The Zipper . https://www.researchgate.net/publication/220676535_The_Zipper

`[2]`：The Derivative of a Regular Type . http://strictlypositive.org/diff.pdf

`[3]`：Generic Zipper: the context of a traversal . https://okmij.org/ftp/continuations/zipper.html

`[4]`：结课论文难找，哪位去他门下当phd的话应该能轻松地看到吧。

## 函数式指针

啊，别误会，这里说的指针并非Pointer, 而是Cursor(光标，游标)。

基本的想法是，我们有一个

