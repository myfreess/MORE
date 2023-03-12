extensible effect (False)
Open Union (True)


周末时我为了不让自己闲着，郑重承诺我要好好读一读ExtensibleEffect的实现(就是freer库)。然后果不其然，我被rust-lang的logo吸引了，枯燥的库代码实现细节有什么好看的, 不如看看小螃蟹卖萌。但是翻开freer的源码，我感觉这个库的实现确实非常magic，到底是怎么想出来这样的设计的呢?

Data.Open.Union

type family Members m r :: Constraint where
    Members (t ': c) r = (Member t r, Members c r)
    Members '[] r = ()

这个模块的作用其实是引入外部的OpenUnionInternal模块并且暴露一个比较Generic的API。大概这段haskell代码很难被阅读，那也没办法，扩展开太多了。

()不一定是Unit。你应该知道haskell有种叫typeclass的东西，它对类型变量有一定的约束作用。比如这样：

format :: Show a => a -> String

但是较真起来，其实还有一种写法。

format :: (Show a) => a -> String

所以()在这里表达的是：一个总是能通过检查的空约束。

emptyConstraint :: () => Bool
emptyConstraint = True

它有什么用？暂且不论，因为略抽象了。让我们荡起双桨......啊不是，另起一段，说一点看似无关的东西。

如何写出这样的一个函数?

isSubsetOf :: Eq a => [a] -> [a] -> Bool

名字已经表明了很多东西，但是我还是坚持描述一下这个函数要做些什么: 给定2个无重复元素的列表(可以当成集合)l r, 如何判断l是r的子集？
比较低效但是直接的实现在此：

isSubsetOf :: forall a . Eq a => [a] -> [a] -> Bool
isSubsetOf l r = foldr go True l where
     go :: a -> Bool -> Bool
     go e b = e `elem` r && b

现在我要说，为什么这里foldr的运算初始值是True呢？ 因为当l是空列表时，l一定是r的子集啊。

回归主线，Members实际上是所谓Type Level Programming的一个例子，所谓Type Level Programming，不外乎通过各种手段在编译期对类型做各种运算，通过复杂的类型设计做到一些在Dependent Type语言里面很简单的事情(逃)。大多数haskell书都会教这种技巧，另有一本专门教授Type Level Programming的书叫做Thinking With Types。考虑到这书实在太厚，而且价格不菲，我找了一篇入门级的blog，看爽了再决定要不要买书。

https://www.parsonsmatt.org/2017/04/26/basic_type_level_programming_in_haskell.html


现在先介绍一下haskell类型系统的一大核心概念：Kind。这个概念来自Lambda演算的一种：system Fω。

http://julytreee.cn/2021/03/20/haskells-kind-system-a-primer/

因为前人之述备矣，所以我不写了。


Kind是类型的身份标志，一般的haskell类型Kind为*

而Maybe a这样的类型构造子，好吧，它并非类型，它要有一个输入的类型参数才能返回一个类型，它的Kind就是*->*了。

使用KindSignatures扩展, 可以允许你写出kind签名，对于type family的使用它常常是有帮助的。

