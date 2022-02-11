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

+ 然后用runStateT去执行bar，拿到一个类型为`IO (X, Y)`的值，再把它传递给foo，foo的返回值还得加个lift到`StateT X IO (X, Y)`

+ 想个法子把`StateT X IO (X, Y)`弄成`StateT X IO Y`, 不过我们知道该干什么，用新状态换掉旧状态，然后简简单单pure个值就好。

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
liftBaseWith $ \runInBase -> ......
```

提问：runInBase是什么？

答：是liftBaseWith动态生成的一个函数，其内部包含了将`StateT s IO a`转换为`IO (s, a)`所需的状态，而且它做的工作也就是这样。

那么，提取初始状态(`s <- get`)的工作被liftBaseWith隐藏，然后封装了一下作为函数提供进来，避免用户徒手触摸状态，挺好的其实。不过在代码行数

