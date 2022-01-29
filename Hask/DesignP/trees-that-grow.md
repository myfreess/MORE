# trees that grow

> 本文为Shayan Najd & Simon Peter Jones论文"trees that grow"的读书笔记 

把时间拨回到1970年代，先贤David Turner在SK组合子上做出的工作和他的语言Sasl ，KRC，Miranda风头一时无两，在函数式世界产生了爆炸般的影响。它们点燃了那一代年轻计算机科学家的热情，让他们迫不及待地投身于函数式的广阔天地。优雅的想法，清晰的表述，还有引人注目且活跃发展的实现。David的工作产生了余音绕梁的效果，例如，Miranda对haskell的设计产生了巨大的影响。

花开两朵，各表一枝。代数数据类型(ADT)和模式匹配相信每个函数式发烧友都不会陌生，如今几乎任何现代的函数式语言都包含它们。其首次出现于1980年ACM Lisp会议上的论文"HOPE - an experimental applicative programing language", 很快它们被ML语言和Miranda吸收。ADT让函数式语言精于定义和处理树形结构，但是

> **trees often cannot grow**

当某个类型被定义好编译过，它将无法通过

+ 增加新的值构造子

+ 为已存在值构造子增加新字段(field)

进行扩展。

对可扩展性的缺乏让手指承受了太多，心智也一同浸入炼狱。例如，每个编译主题广场的中心都立有高大的语法树。编译器常常需要为它们加上额外的装饰，来表达一些在处理过程中需要的信息。一个栗子：名称解析阶段(name resolution phase)在树上添加某个"名字"的全称，类型推导阶段需要在相关节点中存储推导出的类型。我们把这些额外的信息叫做**装饰**，装饰可能出现在已存在值构造子的新字段里，或者/和新的值构造子里。

编译器作者被迫面对两种难以下咽(unpalatable)的选择

+ 为每个装饰过的树定义一个新类型

+ 用一个类型装下所有可能用到的信息

这个困境异常地真实，GHC有一个跨数个编译器阶段的类型`HsSyn`,而TemplateHaskell使用TH.Syntax。对于一些库，比如haskell-src-exts，它们有自己的想法。这些定义的大小往往达到一屏装不下的程度(上百个值构造子)，要靠人来保持不同源码中定义的同步简直就不可能。

> 等下，这是IDE的工作吗？

可以看出来，这是个特化的**表达式问题**，那么Najd和SPJ做了什么就很明显了。**庆贺吧，又tnnd多了一种expression problem的解法！** 不过这次有个好消息，trees that grow这个设计模式有工业级的代码在用 -- GHC在用。

> 赶快用此模式写个玩具编译器，日后CV上可以填编写过n行工业级质量的haskell代码(逃)

## 可扩展ADT定义

trees that grow的基本思路是用Type Family实现对类型定义的编译期改写

```haskell
data ExpX e = LitX (XLit e) Integer
  | VarX (XVar e) Var
  | ...... -- 大概就这样
  | ExpX (XExp e) --额外的值构造子
```

+ `e`是所谓的**扩展描述器**(Extension Descripter), 一般是一个无值类型，它描述具体所使用的扩展策略。

+ 每个值构造子`C`有个额外的字段`(XC e)`,自然，`XC`是个type family，或者类型层面的函数。

例：无需扩展应该怎么写

```haskell
type ExpUD = ExpX UD
data UD
type instance XLit UD = Void
......
type instance XExp UD = Void
-- 由于bottom的存在，如果你真想要只有一个值的类型
-- 那最好还是用Void 
```

机敏的读者`[1]`会发现，如果只是需要一批Void的话，直接不写这些实例就完事了. 但是为了防止在未来的协作者一时兴起给这些个type family加实例，还是应该写出来。


`[1]`: 机敏的读者为原文所言，我当然没那么机敏。顺便，作为一个拼字符串出身的写手，我第一时间想到的是`{-# LANGUAGE CPP #-}`

为何使用`Void`而非`()`? 让我来猜猜，如果使用(), 那么在模式匹配中就可能有人很耿直地用()而非_去做匹配，也许会有十世难遇的坏运气，让他碰见一个用undefined初始化()字段的人，就算这种破事的概率很低，用Void能永久避免，那么为何不呢？

## 改进:引入模式同义词



以上设计的缺陷异常明显

+ 在模式匹配时要用大量的`_`去忽略无需扩展的额外字段(这算不算是滥用undefined了？)

+ 重组的时候还要塞个void进去

```haskell
void :: Void
void = error = "你就不该碰这个"
```

通过`PatternSynonyms`(模式同义词)扩展来避免直接接触危险的bottom

```haskell
pattern LitUD :: Integer -> ExpUD
pattern LitUD i <- LItX _ i -- 解构定义
   where LitUD i = LitX void i -- 构造定义
```

然后的体验大概就是这样

```haskell
incLitX :: ExpUD -> ExpUD
incLitX (LitUD i) = LitUD (i + 1)
incLitX e = e
```

体验也还过得去，当然体验最好的还是直接拜托别人写。



## 几个应用上的例子

+ 类型推导需要额外字段存放推导出的类型

+ 简单的partial evaluation在将redex规约为值之后需要一个新的值构造子存放结果


## 替换值构造子

在某些编译器pass中，对树的改变不限于增加 -- 还有替换。马上举一个让函数式玩家心领神会的例子，柯里化。

对于动态类型语言和Javascript一类的无类型语言，默认柯里化对性能的影响不可忽视。haskell一直都这样(虽然偶有性能不如python的都市传说流出), 但是haskell的编译器可以充分地消除柯里化 -- 应该。

对haskell中的函数，它在编译时有个叫元数(arity)的玩意，记录了其定义的参数数量。当发现一个函数应用链饱和时，就可直接将其替换为参数列表(常常伴随着eta展开)。

大概如下

```
App (App (App f a1) a2) a3 => AppL f [a1, a2, a3]
```

完全展开的函数调用可以避免运行时的闭包，编译出的产物在内存布局上较简单，当然应该要快一点。

替换操作由于haskell没有真正的空类型会让人有点犯难，因为没办法真正弃用旧的`AppX`构造子，只能由编写者通过模块+模式同义词进行控制了

> 模式同义词总是不自觉写成模式别名......

```haskell
type ExpSA = ExpX SA -- 应该是saturated application(饱和应用)的缩写
data SA

-- 省略n个为Void的type instance

type instance XExp SA = (ExpSA, [ExpSA])

pattern AppSA :: ExpSA -> [ExpSA] -> ExpSA
pattern AppSA l ms = ExpX (l, ms)

-- 对外导出API的时候暴露模式同义词即可

```

> 我没想通为什么是替换，此处看来增加就足够了啊，也许是把未饱和的应用也转换了？这下吃了不懂编译原理的亏了.jpg

## 减少type family

这只是个设计模式而非定律。没必要给每个值构造子都配一个type family，按性质和用途分个类然后按需添加就好，比如Lit和Var都是叶节点，那一个XLeaf e也差不多了。不加也随意。

> 任由自身想象堆砌功能和模式妄称可扩展性强和要加点啥就得重写同样是灾难，没有顺风嘲讽log4j的意思。


## 讨论及相关工作

trees that grow所面对的问题和其技术实现都有一些独特的区分点

+ 需要双向可扩展性

+ 泛型只是个添头，而非硬需求。GHC前端的Parser，Renamer和Type Checker都是单态的。

+ 树是预声明的。这样的实现更简单，更少出错。像Row polymorphism这样的手段则往往产生庞大的类型和复杂的错误信息。

+ 不需要什么新技术，只是用了几个已经有的扩展。

与此idiom有相同精神的技术是McBride的Ornaments，但是细节上不太一样。例如，Ornaments维护类型的递归结构，而在GHC中，表示if表达式的那个值构造子有个额外字段放用户重绑定if语法的宏，这就没维护递归结构(better or worse)。

## 短板

+ 低效，增加内存用量

+ 完备性检查(Exhautiveness checker)不友好，不过GHC在论文发表时已经在着手做关于模式同义词的检查和totaliy checker改进了。

+ 大量枯燥乏味的模板代码，可以试试TemplateHaskell。

## 结尾暴论

1980s人们比较关注term，而现在人们更关注类型，说明内存确实是极大丰富了。

**现在正是类型体操的时代！**

David Turner先生，愿您的道行在地上一如行在赛博世界。

小八卦：David Turner看过SPJ的第一篇论文"Yacc in Sasl", 然后David拿出了导师般的态度，把SPJ引向PL之路。








