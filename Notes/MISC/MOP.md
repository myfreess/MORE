# 文本观察/MetaObject protocols: Why we want them and what else they can do

这篇文章只读了开头，但是我感觉用来展示MOP的例子不错。

首先，有一个用在图形应用里面的class叫`position`

```lisp
(defclass position ()
      (x y))
```

用来表示鼠标的位置，在这个例子里，程序中会有非常多的`position`实例，x、y这两个slot(这是commonlisp对类中成员/字段的称呼方式)都会被用到而且对它们的访问要尽可能快。

其次，有一个叫做`person`的类

```lisp
(defclass person ()
     (name age address ......))
```

它可能被用于某种知识表示系统，它有上千个slot，对应某个人可能具有的上千条性质. 

在这个应用场景中，也许还是会涉及非常多的实例，但是每个实例真正被使用的slot并不会很多。除此之外，对slot的访问极少在循环中出现。

显然，对于这两个class，理想的实现方式是不同的。`position`更适合类似数组的紧凑实现，而`person`实现成hash table更合适，节省空间。

典型的面向对象语言对`position`支持得很好，但对于`person`支持则不佳

> 这里我想吐槽一下，把类实现成hash table好像只适合动态类型语言......硬要用Java感觉只能全部退化为Object了

MOP解决此问题的方式是：把一些对象内部的实现细节(比如数据怎么放，多继承怎么决定superclass的优先级)交给用户来控制 - 交权交多少应该看语言设计者怎么想了。目前考虑到性能啥的，直接把对象实例怎么创建交给用户决定还是不太现实，也许可以期待一下partial evaluation之类的技术能对此做出改进。