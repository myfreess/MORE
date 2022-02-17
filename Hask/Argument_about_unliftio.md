# unliftio之争

前几日在阅读一篇与monad-control包有关的博客

https://lexi-lambda.github.io/blog/2019/09/07/demystifying-monadbasecontrol/

作者Alexis在结尾顺带提了一下unliftio这个包，不过言语间有股RDX味。简而言之，unliftio是monad-control的一个竞品，它提供了一个叫做MonadUnliftIO的typeclass。

其作者Michael Snoyman在2017.7.17下午2：51所发的那条twitter里面则是这样介绍的

> UnliftIO, a new, safer approach to unlifting IO actions in transformers

new倒是很容易理解，但是介绍中还提到safer，那么它应该是有些独有的特性？

Alexis肯定是属于不太感冒的那一派了，他觉得：

MonadUnliftIO只不过是一个弱化的MonadBaseControl，其基底Monad只有IO，而扩展Monad只支持一些实际无状态的Monad，比如ReaderT，取而代之的是鼓励用IO Exception和可变引用去代替ExceptT和StateT.

它的设计意图着眼于降低MonadBaseControl所引入的关于状态丢弃的可憎复杂度，以及安全先行。是个高贵的纲领，可惜全无用处。实际上如果真想要这样，开个扩展写个类型别名不就完事？

```haskell
type MonadUnliftIO m = (MonadBaseControl IO m, forall a. StM m a ~ a)
```

实际上`Control.Concurrent.Async.Lifted.Safe`就是这么做的(来自lifted-async库)，只不过它的类型签名看起来不像这个。Alexis的想法是这样的配置让懒得考虑复杂度的人有现成的简单实现可用，而不嫌麻烦的人仍然可以手工实现安全且支持状态的变体。

> 笔者注：从文档看应该只是弄了点新定义, 看起来大概是Forall (Pure m)这样子

```haskell
class StM m a ~ a => Pure m a
-- 隐藏显式forall
type family Forall (p :: k -> Constraint) :: Constraint
```

不过Alexis批到这笔锋一转，又把unliftio官方的比较引用了一下，unliftio的官方文档很中肯，直接说monad-control在功能性上确实更"强", 但是unliftio是为崇尚简洁的用户提供的。那么Alexis也在引用完文档之后痛快承认，就是不爱Plain Simple Haskell，我要更强的Power。

这么一看，并不是unliftio有什么缺陷，而是变成纯粹意识形态大乱斗了。笔者突然理解了为什么有人觉得GHC Haskell社区和C++社区很像。

如果你懒得看MonadBaseControl这一坨，想直接上手连Ek都在用的unliftio，可以考虑这篇教程：

https://www.fbrs.io/unliftio/

祝大家生活愉快，融洽相处。

## 这就完了？

还是有一点未澄清的，Alexis不爽大道至简，但是unliftio自己的宣传词是safe啊。当然一种解释是，没有状态丢弃更利于并发安全了，但是这种拟合出来的解释有很大可能跑偏，跟各位讲个故事吧：

```
2022.2.16 上午12：34

群友甲：What is algebraic about algebraic effects and handlers?

群友乙：一种解释是handler根据tag往表达式里塞副作用很像是代数中用具体的值去换x之类的变量

群友乙：我不知道原意是不是这样，也许这只是一种牵强附会的说法

群友丙：草 他不是在提问

群友丙：那是篇paper
```

群友乙究竟是谁我不说各位也知道了。

于是我强忍着惰性，翻开教程逼迫自己一行一行认真读(迫真)，在3分钟后退火为量子速读，不过还是发现了一点东西：

> Unlike the other modules in this library, the UnliftIO.Exception module not only applies the MonadIO and MonadUnliftIO typeclasses to existing functions. It also changes the semantics of functions available in Control.Exception. This is to simplify proper exception handling, especially in the presence of **asynchronous exceptions**. 

让我大言不惭几句，它很好地贯彻了自己的设计理念, 难能可贵的是并非拿简陋硬充简洁，而是在实现的简明程度和API的易用性上力求取得平衡。

而且还有个unliftio-core，去除了自带的已提升函数实现，可谓是给足了用户面子。它值得被考虑。

