注：本篇偏笔记性质，因为相对于其他模式来说ReaderT算比较"大"的一个模式，笔者系在校学生，缺乏工程经验，只用ReaderT模式做过一个残缺的miniKanren，如果说我有多会，那真是纯纯的欺骗了。

格局打开.jpg

内容主要来自：

https://www.fpcomplete.com/blog/2017/06/readert-design-pattern/

https://chrispenner.ca/posts/mock-effects-with-data-kinds


终于到了激动人心的monad时刻!

ReaderT pattern是一种传递全局状态的设计模式，在haskell社区的不少库中已经投入实用了，比如Haxl，Yesod。我想那就说明至少它并非毫无价值。

> 更正: Haxl没有使用ReaderT模式，而是自定义了一个GenHaxl Monad，它身兼数职，对于Env类型来说，它就是一个Reader。但是Haxl的Env里面存放的是调度器状态，这个好像不算运行时配置哈......

设计步骤如下：

+ 定义核心类型Env，里面要包含所有设计上的“环境变量”。如果有什么需要运行时读取的信息，放进去。如果有需要做Mock测试的函数，放进去。

+ 如果有啥内部要用的可变状态，放在IORef或者TVar之类的可变引用里，同样打包进Env。

接下来定义一个Application类型

```haskell
type App = ReaderT Env IO

-- 用newtype也可以, 不想用IO也可以
```

前面在可变引用处没有推荐STRef，为什么？答案就在App的定义中了，用IO的话，包括输入输出之类的副作用都包括在内了。但是如果你的应用中没有和外部进行IO的需求，用ST当然很好。

基本的设计就是这样，下面给出一些关于ReaderT优势与注意事项的说明，至于是否有道理，还是请各位自行评判吧。


例子：配置Debug级别

在haskell中可以考虑：

+ 1.开CPP扩展，用编译期flag控制

+ 2.全局变量 + unsafePerformIO

+ 3.在main Action中读取配置文件(然后用ReaderT)

1根据经验很糟糕(非笔者言)，2看起来还行，但是：

+ 引入了可能的运行时异常

+ 有时可能开发者会想用一个较高的等级单独运行一小块程序(这个显然2做不到)

+ 对并行不利啊

+ **Every Time you use `unsafePerformIO`, a kitten dies**


避免WriterT和StateT。早期的Yesod中，StateT在Handler类型中用于修改用户会话的值和设置回应头。但是一些问题在实践中逐渐浮现：

+ 运行时异常会让StateT和WriterT当场去世，而开发者却没法追踪到异常发生前的最后可用状态是啥。如果用的是可变引用，起码还能抢救一下。Yesod从ReaderT模式中就得到很多好处，如果response失败，扔了个什么notFound啥的，至少还是可以重新设置一下response header。

+ 在满是状态的应用里，StateT和WriterT实在体现不出什么优势。没错，它们是用纯函数模拟的副作用，但是纯度那种东西有用吗?

+ 并发情况下不保证状态连续性，这个对Web应用很不利。举个例子

```haskell
import Control.Concurrent.Async.Lifted
import Control.Monad.State.Strict

main :: IO ()
main = execStateT
    (concurrently (modify (+ 1)) (modify (+ 2)))
    4 >>= print
```

结果是多少得依赖于当前实现了，当然不否认这代码真的很糟糕，快使用`Control.Concurrent.Async.Lifted.Safe`。

用IORef或TVar啥的不会让问题消失，但StateT也没解决问题啊，它只是试图藏起问题。用可变引用起码会让开发者在写的时候好好想想自己究竟需要什么样的语义，做出更合适的选择。

Michael Snoyman还分享了一个ta觉得会对使用体验有帮助的包：https://www.stackage.org/package/monad-unlift-ref

+ WriterT有内存泄漏，Strict版也有，但是CPS版没有。米田引理，你永远可以相信!

话虽如此，Yesod还是保留了一点WriterT，主要在WidgetT里面，保留的理由也非常耿直:

+ 不需要保留异常前的状态，出问题直接发个error page就完事。

+ widget构建没什么需要并发的理由(There's no good reason to use concurrency when constructing a widget.)

+ widget构建这个行为自身其实在概念上还算纯的

+ 虽然很担心内存泄漏，但是经测试WriterT在这个场景最快。

如果需要大量手写lambda打包进ReaderT，那体验也不会太好。mtl对此的做法是引入一种叫做「Tagless Final」的设计模式，很有名，至少比手写好用。

https://zhuanlan.zhihu.com/p/53810286

https://zhuanlan.zhihu.com/p/20834962

肉眼可见的缺陷就是类型签名体积膨胀。

抛开Tagless Final不谈，mtl的做法是定义了一个MonadReader class，所有能提供类似reader的读取变量功能的monad都可以实现它的实例。然后在do 里面只用这个class提供的函数，就可以避免写出显式的ReaderT构造子和lambda。如果将来有需要，还可以切换到其他API兼容的monad(逃

还可以结合之前介绍过的「HasIt」模式使用

```haskell
data Env = Env
  { envLog :: !(String -> IO ())
  , envBalance :: !(TVar Int)
  }

class HasLog a where
  getLog :: a -> (String -> IO ())
instance HasLog (String -> IO ()) where
  getLog = id
instance HasLog Env where
  getLog = envLog

class HasBalance a where
  getBalance :: a -> TVar Int
instance HasBalance (TVar Int) where
  getBalance = id
instance HasBalance Env where
  getBalance = envBalance

-- 然后函数签名就会变成这样

modify :: (MonadReader env m, HasBalance env, MonadIO m)
       => (Int -> Int)
       -> m ()

```

Chris Penner在博客中把MonadIO批判一番，说你们这个啊，**naive!**  IO太宽泛了，数据库连接是IO，打个Log是IO，读个Ref还是IO，得有点区别。然后又用Tagless Final弄了一堆MonadDB 、MonadFileSystem、MonadLogger啥的，再加上Phantom Kind对副作用使用加以限制(类型签名里面没IO了，能用啥副作用都被给定的typeclass规定好了)。如有需要也可参考。
