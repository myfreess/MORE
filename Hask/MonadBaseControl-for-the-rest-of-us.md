# MonadBaseControl是什么？

笔者在2021年上半年时见到一张关于haskell中ReaderT设计模式的梗图，好吧，抽象的尽头终将是`Config -> IO a`，毕竟现在流行的是大道至简嘛。但是前面还出现过`MonadBaseControl`, 这是一个来自monad-control包的typeclass，看这个MonadXXX的固定句式就知道不是什么好说话的角色。

然后一位见得多了的群友给上台拿衣服的笔者推荐了这篇博客：

https://lexi-lambda.github.io/blog/2019/09/07/demystifying-monadbasecontrol

推荐的时间应该是在夏季，当时囫囵吞枣地过了一遍，及到笔者开始写这篇笔记的时候(2022/2/26)，已经不知道当初看了些啥了。最大的可能是当初我就没看懂，现在炒夹生饭希望是为时未远。

顺便我也挺好奇这玩意究竟是用在什么地方，所以还搜索了另外几份教程。

https://www.yesodweb.com/book/monad-control

https://www.parsonsmatt.org/2017/11/21/monadbasecontrol_in_five_minutes.html

五分钟速成这种标题看起来当然是舒心多了，但是既然没有考试压力，不妨都看一看。

## 直觉

在提到`MonadBaseControl`之前，有必要提一提它的前驱`MonadBase`，一个较简单些的typeclass。

```haskell
class (Applicative b, Applicative m, Monad b, Monad m) => MonadBase b m | m -> b where
    liftBase :: b α -> m α

-- 来自transformers-base库，因为用的标准还是haskell98所以前置约束中有看起来冗余的Applicative
```

liftBase方法的意图非常简单，得到一个基底(base, 对应类型变量b)monad的值`b a`，则有办法将其转换为一个类型是`m a`的值。

而`MonadBaseControl`所描述的是`MonadBase`的一个子集, 它所作的工作大致如下：

+ 我们有个函数`foo :: b a -> b a`

+ 还有个值`bar :: m a`

要怎样把bar喂给foo呢？

偷看一眼`MonadBase`的实例，会发现基本只有俩种情况，要么b和m就一样，要么m是b加上某个MonadTransformer，凝噎了。

暂时着眼于较具体`[1]`的例子来看看

```haskell
foo :: IO a -> IO a

bar :: StateT X IO Y
```

`[1]`: 虽然具体了很多，但是foo函数仍然保持多态，因为`MonadBaseControl`的API不支持单态函数(起码参数得多态)

MonadTrans这东西......好增不好减，那也是为什么会有人(fpcomplete是首创吗？)搞出糙平快的ReaderT pattern，lift和boilerplate写多了确实比较麻。仅以此例，我们要做的是一些很乏味的重复性工作：

+ 首先提取出StateT的状态

+ 然后用runStateT去执行bar，拿到一个类型为`IO (Y, X)`的值，再把它传递给foo，foo的返回值还得加个lift到`StateT X IO (Y, Xs)`

+ 想个法子把`StateT X IO (Y, X)`弄成`StateT X IO Y`, 不过我们知道该干什么，用新状态换掉旧状态，然后简简单单pure个值就好。

> 个人建议用pure代替return，这可以为未来的更改提供方便

实现简单而没味。

```haskell
foo' :: StateT s IO a -> StateT s IO a
foo' m = do
-- 消费降级
  s <- get
  (v, s') <- lift $ foo (runStateT m s)
-- 状态复位
  put s'
  pure v
```

从这个例子也能看出为什么需要foo对它的参数多态了，想必正常的处理IO的函数不会考虑到得到的值是买一送一，还附带一个状态这种事。且单态会让foo具有在状态上做些具体操作的能力，实际上它不应该对状态多管闲事的，对吧。

`MonadBaseControl`的想法是把以上boilerplate分成俩个函数：`liftBaseWith`和 `restoreM` 

liftBaseWith是一个回调式的API,一般配合lambda用，就像这样

```haskell
liftBaseWith $ \runInBase -> foo (runInBase bar)
```

提问：runInBase是什么？

答：是liftBaseWith动态生成的一个函数，其内部包含了将`StateT s IO a`转换为`IO (a, s)`所需的状态，而且它做的工作也就是这样。

那么，提取初始状态(`s <- get`)的工作被`liftBaseWith`隐藏，然后封装了一下作为函数提供进来，避免用户徒手触摸状态，挺好的其实。不过在代码行数上看不出什么助益，大概只是避免犯错.

而`restoreM`所需做的就是对附加了状态的值做一个状态复位，这个简单。现在来写一下上面那段示例的等价程序

```haskell
do
  s <- liftBaseWith $ \runInBase -> foo (runInBase bar)
  restoreM s
```

也不用弄个无聊的新函数了，挺好。

不过，MonadBaseControl的实例可有不少，所以还是让我们来看看具体的函数签名长什么样

```haskell
class MonadBase b m => MonadBaseControl b m | m -> b where
    type StM m a :: *
    liftBaseWith :: (RunInBase m b -> b a) -> m a
    restoreM :: StM m a -> m a
```

RunInBase是怎么回事呢，原来是个类型别名

```haskell
type RunInBase m b = forall a. m a -> b (StM m a)
```

就算这么说，还是不好懂啊。

别急，引入type family的唯一理由是 -- 适配具有不同状态的Monad Transformer。

在前文的例子，从StateT到IO的转换使得参数类型由a变成变成了`(a, s)`, 可以想象的是，如果换个MonadT那么参数类型又要不一样了。但是，我们可以知道的是，最终产物的类型一定由一个monadT和一个原始的类型参数所决定，够了，那就是使用type family的理由。

在monad-control库中，`StM m a`被称为是m在基底monad(一般称为`b`)上附加的单子化状态(monadic state).

手工推导出这个monadic state类型对大多数读者而言都很轻松，但是这样的工作仍然有点繁重了(迫真)，所以monad-control库的作者还准备了一点甜点

假如你有个monad transformer叫T

那么`instance MonadBaseControl B m => MonadBaseControl B (T m)`只要如下操作即可

```haskell
instance MonadBaseControl b m => MonadBaseControl b (T m) where
    type StM (T m) a = ComposeSt T m a
    liftBaseWith f   = defaultLiftBaseWith
    restoreM         = defaultRestoreM
```

这就对啦，再封装个Template 就更加自动化轻松又愉快(误)

## 例子

虽然作者已经给出了非常自动化的实例定义流程，但是还是让我们来思考一下自己写StM的type family实例要怎么办。

首先对基底那肯定是Identity啊

```haskell
StM IO a = a
```

然后对于StateT, 加一个递归

```haskell
StM (StateT s m) a = (StM m a, s)
```

收工，拿`StateT s IO a`套进去试试·看，刚刚好，那就说明......

什么也说明不了，一个巧合。

这个m完全可以是基底monad上套了n层Monad transformer，想象一下

```haskell
StateT s (MaybeT IO) a
```

那么转化产物的类型实际上是

```haskell
IO (Maybe (a, s))
```

就观察MTL库的结果来看，类型上处在外层的monadT却会处于控制流的内层。而产物类型的层次从直觉上应该和控制流一致，所以StM的实现应该是一个层次翻转的累积过程。

> 暴论一下，我不知道这个解释能否自圆其说

尾递归，当然要尾递归啦。

```haskell
StM IO           a ~ a
StM (MaybeT   m) a ~ StM m (Maybe a)
StM (StateT s m) a ~ StM m (a, s)
```

## 为什么是回调风格

让liftBaseWith直接对外提供runInBase会怎样？

```haskell
  runInBase <- askRunInBase
  restoreM =<< liftBase (bar (runInBase ma) (runInBase mb))
```

很遗憾，做成回调式API的原因不是回调更好，而是haskell的类型系统限制使得这里只能这样......

来看看这个runInBase的类型

```haskell
runInBase :: forall a. m a -> b (StM m a)
```

也许我们会需要把它拿给这样的函数去用

```haskell
baz :: IO b -> IO c -> IO (Either b c)
```

此处就会有需要让a被实例化为俩个不同的类型变量，这样的需求显然是合理的，为了让此处的调用合乎类型，那askRunInBase的类型就会是

```haskell
askRunInBase :: MonadBaseControl b m => m (forall a. m a -> b (StM m a))
```

很遗憾，不得行。haskell中类型构造子的参数只能是一个单态类型`[0]`，RankNTypes扩展只给`(->)`构造子开了后门，所以liftBaseWith是可行的。

`[0]`: 直到GHC9.2加入了一个成熟的ImpredicativeTypes扩展，这一切才有了可能。改变这一切的那个类型推导算法叫**Quick Look**。

> https://ghc.gitlab.haskell.org/ghc/doc/users_guide/exts/impredicative_types.html

当然了，还有一条路是newtype

```haskell
newtype RunInBase b m = RunInBase (forall a. m a -> b (StM m a))
```

但是这会带来很多的语法噪音，不知道有什么人会喜欢它。monad-control库直到1.0.2.3也没提供个类似的类型定义，也许未来会有。

> 2022.2.15, 版本 1.0.3.1, 没有。

