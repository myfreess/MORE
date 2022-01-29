### haskell缺陷之一

内容来源：

https://www.snoyman.com/blog/2020/10/haskell-bad-parts-1


### foldl

不止一个人想过要把Prelude和Data.List里的foldl函数换成foldl'了。在长期的实践中，haskeller们实在没找到有什么合理的使用场景需要惰性求值+尾递归，顺便还有通过foldl定义的sum，product。尽管现在的foldl使用了很多优化措施，但是惰性就是惰性。

有些人认为惰性的foldl在Vector上表现很好，所以应该保留。但是这应用场景说实在的太窄了。应该把惰性版本的foldl命名成foldLazy才对。(原作者的命名是foldlButLazyIReallyMeanIt)

### sum/product

额，上面不是说过了？

实际上sum/product现在是Foldable类型类下属的函数了，针对list，它的确是使用foldl的特化实现，但是对于一些只实现了最小实例的类型构造子t，还是foldMap搞定。现在换成树形递归哪里不好？和`+,*`有点不搭，因为`+,*`它们对自己的参数都是急切求值的。我无意指责+和*, 但是sum/product实现成累积器模式会比较好。转换为list再用foldl'处理会产生不必要的中间列表，大概是时候看看砍伐律/酸雨律了。

或许有人会说，GHC会做这些优化的! 不要这样，期待GHC会把烂出水准的代码优化成一般般的代码，还不如现在就去改行来的爽利。

### partial function

现在的Prelude不讨人喜欢，除了著名的FTP(Foldable和Traversable进Prelude), 还有一点就是偏函数的存在了。比如head，比如tail。Prelude中的偏函数默认是没有调用栈追踪的(社区意见不统一，反正问题很捉鸡), 崩了的话，就像这样:

```
test: Prelude.head: empty list
```

哪一行的调用出了问题？不知道。具体的调用链？不知道。反正只知道是head搞非暴力不合作。

目前有这么个建议(无意间看到的)，对于自己写自认为安全的偏函数，在不可能发生的分支那里用error把你认为为什么这个分支不可能match到的理由打印出来, 举个例子

```haskell
halfOfEvens :: Rational -> [Rational] -> [Rational]
halfOfEvens n = map (\d -> n `divEx` d) . filter (/= 0)
  where
    x `divEx` 0 = error
      "This should be impossible: We already filtered the list to remove zeroes"
    x `divEx` d = x / d
```

例子及建议来自：https://github.com/freckle/guides/blob/main/haskell-best-practices.md

如果要加调用栈追踪, 最常见的方法是使用GHC.Stack.HasCallStack。实际上error函数默认就会打印CallStack，但可是如果想显示完整的CallStack，就不得不顺着调用链给所有要用到的函数都加上HasCallStack这个typeclass的约束。例子抄了

```haskell
f :: HasCallStack => IO ()
f = error "bad bad bad"

g :: HasCallStack => IO ()
g = f

h :: HasCallStack => IO ()
h = g
```

对h求值，能从h追踪到g追踪到f，可是如果把hg的约束去除，就只能知道是f出错了。

更多细节在文档里:https://ghc.gitlab.haskell.org/ghc/doc/users_guide/exts/callstack.html

HasCallStack会造成一点性能上的损失, 我也只是听说，还是用着吧。

已经有人在做一些自定义的Prelude，力图让偏函数滚出haskell共和国。

### Data.Text.IO

这个模块里读写文本文件的函数会跟随主路线反复横跳，编码用啥依照环境变量来(Locale Sensitive)。识相的话，还是离这号人远点吧。对于英语用户，it just works。对于中文用户，Windows的默认编码是GBK。

如果说有什么比它还烂，我怀疑是Data.Text.Lazy.IO。一定要使用Text的话，建议搭配此模块：https://hackage.haskell.org/package/with-utf8

没被捆在Text的战车上，可以考虑rio和Z.haskell。

2021.9.10 Update

> https://github.com/haskell/text/pull/365 Switch internal representation to UTF8

喜报，该pull request已经merge


### bracket

几乎每个星期的编程任务中都有这样一种：获取资源，使用资源，释放资源。在函数式编程中我们可以用高阶函数稍微做一点抽象。

```haskell
bracket :: IO a        -- computation to run first ("acquire resource")
        -> (a -> IO b) -- computation to run last ("release resource")
        -> (a -> IO c) -- computation to run in-between
        -> IO c
```

非常好! 为它写个实现吧

```haskell
bracket r finish customer = 
    do res <- r
       result <- customer res
       finish res
       return result
```

ok，完全能过类型检查! 但是，稍微等等，我们带入一个实际例子看看。

```haskell
bracket
  (openFile "filename" ReadMode)
  hClose
  (\fileHandle -> do { ... })
```

openFile，常用又常见，但是因为用haskell写实际应用的没几个，好像大家都忘了，打开文件这种操作是有机会引发异常的。

好吧，这个简单的实现肯定是不好用了。haskell的异常在Control.Exception中定义，当然，提供了bracket函数, 它会保证如果在获取/消费资源时内部抛出异常，那么收尾工作一定会完成。看起来事情就这样结束了，但是，等等，朋友要了解一下Async Exception吗？

异步异常是haskell内一种类似Unix中信号的机制，它到底能干嘛？好吧，闲话少说，haskell中的每个线程都有一个ThreadId，只要拿到一个线程的id，就可以用throwTo函数向它发送一个异常。当然了, bracket本身是一个提供“保护”的函数, 所以在进行运算时它会暂时地“masking”掉对外部异步异常的接收，这就完事了。

完事了吗？

还是没有，这里不得不说一下，在现在的实现中对异步异常的“masking”分为可中断/不可中断2种，不可中断的masking就是masking，而可中断的masking(interruptible masking)同样会拒绝异常，但是假如内部运算是阻塞的，那拒绝的态度可能就没有那么坚决。那么Control.Exception.bracket用的啥呢......

对，它使用可中断的masking，所以释放资源的操作如果会阻塞，那它实际上是有概率被外部发来的异常中断的。比如说假如你弄了一堆tmp文件然后打算在释放资源的时候删掉，运气不好就没删掉一直留着了。

具体哪些操作会被打断请看文档。

https://hackage.haskell.org/package/base-4.15.0.0/docs/Control-Exception.html

鉴于笔者到现在既没有用过多线程也木有用过bracket，这对笔者本人应该不是个问题。

不过，至少以下操作是不可中断的。

+ Data.IORef模块中所有涉及IORef的操作

+ 没用retry的STM transactions

+ Foreign模块全体

+ Control.Exception除throwTo以外的操作

还有4个特例，实在头疼，不一一举出了。

异步异常论文在此，这个真的是haskell特色。另外论文的实现和实际GHC中使用的实现不一样，领会下思路即可, 不要背API(也别问我是不是犯傻背了论文里的API然后被编译器毒打)。

https://www.researchgate.net/publication/220752313_Asynchronous_Exceptions_in_Haskell


