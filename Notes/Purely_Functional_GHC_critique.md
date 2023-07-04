# 文本观察/模块化GHC

概括：Sylvain Henry在尝试使用GHC内部设施时惊骇地发现，其内部组件杂乱无章且无明显边界。最后他与几个协作者一同写下了论文**Modularzing GHC**, 旨在揭示是什么让GHC这样一个由函数式语言编写的大型项目变成充满状态、处处耦合、缺乏可组合性的程序，以及思考如何重构GHC。

第三节*Some design defects in GHC*主要讲述作者在GHC的code base中挖出的糟糕设计，4、5节以领域驱动的思想重新设计GHC。

GHC 9.6开始逐渐按照该论文的设计重构。

## 3.1 Shotgun parsing

*Shotgun parsing*指一种将文本分析和输入验证混合在一起的反模式，这和类型驱动开发中著名的口号*parse, don't validate*相悖。一个典型的例子是GHC如今的构建机制**Backpack 16**. 论文中的设计需要改动Module类型的定义，但是实现者选择用特殊值来代替，理由是改定义会让一些现有的函数变成偏函数，并且需要大量的重写工作。结果就是出现这样的函数：

```haskell
loadInterface :: SDoc -> Module -> WhereFrom -> IfM lcl (MaybeErr SDoc ModIface)
loadInterface doc_str mod from
| isHoleModule mod
-- Hole modules get special treatment
= ...
```

评价为：**it is saddening to see this kind of code in the flagship Haskell codebase**

注：目前(2023/6/2)的Module类型定义是`type Module = GenModule Unit`, GenModule类型的定义则是

```haskell
data GenModule unit = Module
   { moduleUnit :: !unit       -- ^ Unit the module belongs to
   , moduleName :: !ModuleName -- ^ Module name (e.g. A.B.C)
   }
   deriving (Eq,Ord,Data,Functor)
```

isHoleModule函数也改了，类型变成了`GenModule (GenUnit u) -> Bool`

## 3.2 Command-line flags (DynFlags)

GHC里面表示命令行参数的是一个巨大的record，类型名叫DynFlags。这个名字最早出现在[35fb1e38](https://gitlab.haskell.org/ghc/ghc/-/commit/35fb1e382c4561fbd8e1ad66cd515c706e62be41). 需要显式传递并且在编译时可能发生变化的flag称为动态flag，其余的则是静态flag。原则上来讲静态flag应该用全局变量存放，但是直至这篇论文写作时仍有三个例外:`-dppr-debug, -dno-debug-output, -fno-state-hack`.

### 3.2.1 Layering Issues

DynFlags不仅存在于GHC体内，还传染到了ghc-lib中。一个非常合适用于说明问题的例子是Core语法中用于创建一个`Int#`字面量的函数

```haskell
-- | Creates a ’Literal’ of type @Int#@
mkLitInt :: DynFlags -> Integer -> Literal
```

事出有因 - `Int#`具体多大是由目标架构决定的。目标架构信息在哪里呢？自然是由GHC从配置文件中读取到某个DynFlags啦。

一个API用户很可能已经先验地了解了目标架构信息，这种情况下多半会瞎编一个`DynFlags`，里面除了平台信息剩下的全是随机编造的垃圾数据，这已经足够不友好了，但更糟糕的是这一接口甚至是破损不堪的！当`DynFlags`被全局共享时，bug将偷偷飞进用户的鼻子里。

可以想象，其他调用`mkLitInt`的函数将不得不加上一个`DynFlags`参数(这些代码需要跨平台), 这绝对不利于模块化。

### 3.2.2 Shotgun parsing DynFlags

许多具体行为依赖命令行flag信息的函数选择用谓词去处理`DynFlags`参数，调用链过深之后很难搞清楚究竟是哪个函数真的在使用`DynFlags`. 那么出现这样的注释也不奇怪：

```haskell
 , hscs_iface_dflags :: !DynFlags
 -- ^ Generate final iface using this DynFlags.
 --
 -- FIXME (osa): I don’t understand why this is necessary,
 -- but I spent almost two days trying to figure this out
 -- and I couldn’t .. perhaps someone who understands this
 -- code better will remove this later.
```

有些`DynFlags`值被暂存供后续使用的理由便是没人搞清楚它究竟被如何使用，在这篇论文写就时，`DynFlags`已经是一个有145个字段且许多字段基数大于2的大Record类型了，以某个格外突出的字段为例，它同构于191个Boolean所组合而成的Tuple，足足2^191种可能的取值啊。

这种大量使用谓词的编程风格使得模式匹配检查器形同虚设，另外测试也变得相当麻烦。

### 3.2.3 When immutable becomes mutable

本来`DynFlags`这东西应该是不可变的，奈何在实践中真的有些东西(例如`--dynamic-too`)需要它可变。时至今日，你还能在`GHC.Tc.Types`里面看到这样的注释：

```haskell
data Env gbl lcl
  = Env {
         env_top :: !HscEnv, -- Top-level stuff that never changes
```

这是一个谎言，因为`HscEnv`里面有个`DynFlags`字段。

### 3.2.4 Why not make DynFlags implicit?

2012年一个把`DynFlags`包装到Reader Monad里面的点子被加进GHC用于各种pretty-printing,这就是`SDoc`类型。这个想法的初衷可能是想着让大家眼不见心不烦，但结果让GHC变得更加stateful了。

最初加入`DynFlags`只是需要使用一些presentation方面的选项信息(比如要不要打印uniques)，但是既然里面的设置(比如平台)和编译器状态(例如已加载包的信息)也可以访问，最终它们也被使用了......

只需要一个例子就能看出这做法不妥当。`SDoc`可能会作为异常信息被返回，如果客户端代码给出的`DynFlags`值没有包含正确的设置/编译器状态信息，那么打印出来的错误信息也是错误的！或者干脆整个打印进程由于偏函数直接崩溃。

论文作者花了一些功夫(spent countless hours of work to revert this)试着修正这个设计，例如他们设计了一个叫做`OutputableP`的typeclass用来提供打印信息时需要的上下文信息(比如目标架构)，后发现在2012年有一个叫`PlatformOutputable`的typeclass被删除了，和`OutputableP`非常相似。

### 3.2.5 The genesis of a global mutable DynFlags variable

有些使用`SDoc`的函数没法拿到`DynFlags`, 例如一些被用于"静态上下文"中的跟踪函数。

最简单的解决方案当然是直接提供一个

```haskell
commit ab50c9c527d19f4df7ee6742b6d79c855d57c9b8
Date:  Tue Jun 12 18:52:05 2012 +0100
   Pass DynFlags down to showSDoc
-- tracingDynFlags is a hack, necessary because we need to be
-- able to show SDocs when tracing, but we don’t always have
-- DynFlags available. Do not use it if you can help it.
-- It will not reflect options set by the commandline flags,
-- it may have the wrong target platform, etc. Currently it
-- just panics if you try to use it.
tracingDynFlags :: DynFlags
tracingDynFlags = panic "tracingDynFlags used"
```

直接崩溃有点太过分了，很快有人进行了改进，只有一些很少被使用的字段被访问时会崩溃。

> 原作者的说法很有意思，他说"this got fixed on the very same day"

```haskell
commit 37f9861ff65552c2bb6a85c3b27e0228275bc0b6
Date:   Tue Jun 12 23:29:53 2012 +0100
   Make tracingDynFlags slightly more defined

   In particular, fields like ’flags’ are now set to the default,
   so at least they will work to some extent.

-- Do not use tracingDynFlags!
-- tracingDynFlags is a hack, necessary because we need to be
-- able to show SDocs when tracing, but we don’t always have
-- DynFlags available. Do not use it if you can help it.
-- It will not reflect options set by the commandline flags,
-- and all fields may be either wrong or undefined.
tracingDynFlags :: DynFlags
tracingDynFlags = defaultDynFlags tracingSettings
   where tracingSettings = panic "Settings not defined in
tracingDynFlags"
```

很快有些尝试访问setting的函数暴露出问题，再改

```haskell
commit cfb038de5df3fd2521987c143b3e5257d5d20055
Date: Fri Jul 20 19:10:14 2012 +0100

Make tracingSettings have just enough information to get debug output printed
I suspect I have done the wrong thing; I hope someone can improve.

{-# OPTIONS_GHC -fno-warn-missing-fields #-}
-- So that tracingSettings works properly

tracingDynFlags :: DynFlags
tracingDynFlags = defaultDynFlags tracingSettings

tracingSettings :: Settings
tracingSettings = Settings { sTargetPlatform = tracingPlatform }

tracingPlatform :: Platform
tracingPlatform = Platform { platformWordSize = 4, platformOS = OSUnknown }
```

不出所料地，又有人躺枪了：[#7304 — arm-linux: Missing field in record construction DynFlags.sPlatformConstants](https://gitlab.haskell.org/ghc/ghc/-/issues/7304)

够了，够了，后面还折腾了两轮，结果也没咋好。

### 3.2.6 When immutable really becomes mutable: GHCi

ghc-lib在设计时没有充分考虑到GHCi这种在运行时还可以set/unset命令行flag的程序

```haskell
$ ghc-8.10.5 --interactive
GHCi, version 8.10.5: https://www.haskell.org/ghc/ :? for help
> :set -fexternal-interpreter
> 1
ghc: ghc-iserv terminated (-11) <-- segmentation fault
Leaving GHCi.
```

让问题更加艰难的是这个世界上有两种flag，一种是交互输入的，另一种是源码里用`{-# OPTIONS_GHC ...... #-}`传递的。它们被要求和谐相处，但是往往天不遂人愿。

## 3.3 Top-level session state (HscEnv)

`Hsc`是ghc-lib中一个用于顶层会话状态的数据类型

```haskell
newtype Ghc a = Ghc { unGhc :: Session -> IO a }

-- | The Session is a handle to the complete state of a
-- compilation session. A compilation session consists of
-- a set of modules constituting the current program or
-- library, the context for interactive evaluation, and
-- various caches.

data Session = Session !(IORef HscEnv)

-- | HscEnv is like Session’, except that some of the fields are
-- immutable.

-- An HscEnv is used to compile a single module from plain Haskell
-- source code (after preprocessing) to either C, assembly or C--.
-- It’s also used to store the dynamic linker state to allow for
-- multiple linkers in the same address space. Things like the
-- module graph don’t change during a single compilation.

-- Historical note: \"hsc\" used to be the name of the compiler
-- binary, when there was a separate driver and compiler.
-- To compile a single module, the driver would invoke hsc on
-- the source code... so nowadays we think of hsc as the layer
-- of the compiler that deals with compiling a single module.

data HscEnv = HscEnv
    { hsc_dflags :: DynFlags
    -- ^ The dynamic flag settings
    , hsc_IC :: InteractiveContext
    -- ^ The context for evaluating interactive statements
    , ...
```

注释已经过时了，2005年后的`Hsc`表示的是全局的GHC会话(可以是交互式的也可以不是)可变状态。`hsc_IC :: InteractiveContext`字段存放的是GHCi的状态，其中还包含着GHCi自己的一个`DynFlags`。

### 3.3.1 HscEnv’s DynFlags

`OPTIONS_GHC`这个pragma有其内禀的破坏性，它被设计用于设置模块特定的flag(通过修改`HscEnv`内的`DynFlags`)，但是有些flag是必须全局生效的。

```haskell
{-# OPTIONS_GHC -static #-}
module Test where

main :: IO ()
main = putStrLn "Hello World"
```

```
$ ghc-9.2 Test.hs -dynamic
[1 of 1] Compiling Test
Test.hs:2:8:
  error: Bad interface file: .../base-4.16.0.0/Prelude.dyn_hi
  mismatched interface file profile tag (wanted "", got "dyn")
```

### 3.3.2 HscEnv’s caches

`HscEnv`被当作一个全局的状态存储器使用。例如它包含多个从磁盘读取的模块接口缓存(external package state, EPS)以及会话过程中生成的模块接口(home package table, HPT).

首当其冲的问题是只有一个环境在跨平台/多目标编译时该怎么区别不同环境下的模块(e.g. host vs target, profiling vs
non-profiling, dynamic vs non-dynamic).

进一步地说，在一个单一的可变环境上执行隐式的副作用怎么做对是个大问题。例如，读取模块接口的顺序是很重要的，悲剧性的事实是实践中往往总有点地方没搞对：

**信息太少**

如果第一次读入的模块包含`-fignore-interface-pragmas`这个flag(或者使用` -O0 `编译隐式开启/关闭这个flag)，接口文件会被部分读入缓存(据说是因为性能原因)，下一个读取该接口的模块则不出意外地读取到残缺的接口信息(即使该模块未使用此flag)

+ [#8635 — GHC optimisation flag ignored when importing a local module with derived type classes](https://gitlab.haskell.org/ghc/ghc/-/issues/8635)
+ [#9370 — unfolding info as seen when building a module depends on flags in a previously-compiled module](https://gitlab.haskell.org/ghc/ghc/-/issues/9370)
+ [#13002 — :set -O does not work in .ghci file](https://gitlab.haskell.org/ghc/ghc/-/issues/13002)
+ [#20021 — Optimization options (esp. -O2) in OPTIONS_GHC pragma can cause frustrating behavior](https://gitlab.haskell.org/ghc/ghc/-/issues/20021)
+ [#20056 — -fignore-interface-pragmas doesn’t work well with --make](https://gitlab.haskell.org/ghc/ghc/-/issues/20056)

**信息太多**

多了就会泄漏, 泄漏给本不应该得知这些事的模块。

+ [#2182 — GHC sessions (--make, --interactive, GHC API) erroneously retain instances](https://gitlab.haskell.org/ghc/ghc/-/issues/2182)
+ [#8427 — GHC accepts invalid program because of EPS poisoning](https://gitlab.haskell.org/ghc/ghc/-/issues/8427)
+ [#9422 — EPT caching on --make can make spurious instances visible](https://gitlab.haskell.org/ghc/ghc/-/issues/9422)
+ [#13102 — orphan family instances can leak through the EPS in --make mode](https://gitlab.haskell.org/ghc/ghc/-/issues/13102)

**信息有误**

在隐式加载顺序改变后有些信息已经失效(例如在GHCi里面reload一次)，但是Cache忘了更新。

> 经典计算机科学难题之cache invalidation

+ [#2404 — GHCi :r does not reset imported class instances](https://gitlab.haskell.org/ghc/ghc/-/issues/2404)
+ [#9729 — GHCi accepts invalid programs when recompiling](https://gitlab.haskell.org/ghc/ghc/-/issues/9729)
+ [#10420 — “Care with plugin imports” is wrong / orphan RULE visibility](https://gitlab.haskell.org/ghc/ghc/-/issues/10420) (rewrite rules defined into plugins leaking into compiled modules!)

以上列出的issues有些直至论文成文时尚未关闭。

### 3.3.3 Code reuse

就跟`DynFlags`一样，`HscEnv`也在GHC的代码库里到处繁殖，这对GHC子组件(type-checker, renamer, desugarer (HsToCore), Core optimizer,
most code generators)的复用产生了非常大的妨害.

举个例子，如果想把某个模块编译成ByteCode然后打印出来(用于debug或者啥的)

```haskell
byteCodeGen :: HscEnv
            -> Module
            -> [StgTopBinding]
            -> [TyCon]
            -> Maybe ModBreaks
            -> IO CompiledByteCode
```

没有文档，但是类型签名看起来还是好懂的，`Module`是目标模块，`[StgTopBinding]`是这个模块中top-level binding的STG表示(STG就是那个"spineless, tagless G-machine")列表, `[TyCon]`是此模块内的类型构造子列表，`Maybe ModBreaks`看起来就有些怪，可能是某些关于断点的数据。

但是为什么需要一个`HscEnv`呢？为什么结果包裹在一个IO Monad里面？看起来`HscEnv`提供了

+ Logger，用于向stdout/stderr输出log以及生成dump file
+ DynFlags, 用于获取目标信息(操作系统，要不要做profiling，字大小，栈大小限制，栈大小，目标架构寄存器)
+ Interpreter, 用于确定解释方式(要不要profiling)，分配string字面量(MallocStrings命令)，分配FFI调用所用的wrapper(PrepFFI命令)

唯一确定哪些字段被使用了的办法就是顺着调用链做深度优先搜索。

在此处，代码生成器必须有一个解释器才能正常工作，这非常反模块化，而且还没有解释器命令能释放被分配的string字面量......

## 3.4 Interpreter

GHCi在没有经过合适重新设计的情况下便被用于实现TemplateHaskell和编译器插件。

解释器既支持ByteCode也支持native code，后者要难一点，不仅因为各个平台不太一样，还因为GHC在同一平台有多种ABI(e.g. with profiling enabled or not, dynamically linked or not, etc.)

### 3.4.1 Internal interpreter

曾经GHC只有一种解释器，称作*internal interpreter*, 它通过所谓的"运行时链接"运行native code(这个功能由GHC的RTS实现，所以依赖于GHC).

此方式需要native code的ABI和当前所用的GHC一致，所以internal interpreter对跨平台编译/用profiling等方式编译出的native code无能为力。

### 3.4.2 Avoiding the use of the interpreter

一种不是办法的办法是不使用解释器，这是GHC它自己采用的办法。

假设我们已经有了一个GHC程序(ghc-stage0),它可以产出old_abi的对象代码，现在用它来编译一个新GHC(ghc-stage1), 它可以产出new_abi的对象代码。显然，ghc-stage1无法使用internal interpreter，因为它编译出的对象代码和它本身的ABI不一致。但是因为GHC的代码中没有需要internal interpreter的特性，可以用stage1编译出stage2，stage2可以使用internal interpreter，它也是一般情况下分发出去的二进制。

> 悲哀的是对于跨平台编译，这个问题基本没办法解决，直至论文成稿时跨平台的GHC分发的仍是stage1 : https://gitlab.haskell.org/ghc/ghc/-/issues/19174
>
> cross-compilers don’t support compiler plugins : https://gitlab.haskell.org/ghc/ghc/-/issues/14335

### 3.4.3 Working around “ways”

GHC可以根据选项产出ABI不同的对象代码

+ 使用动态链接或者不用
+ 支持profiling或者不支持
+ 使用额外的debug断言或者不使用
+ 使用不同的堆对象表示(e.g. tables_next_-to_code)

有些选项是编译期决定的(e.g. tables_next_-to_code),另一些可以运行时配置的选项被称为"ways".

当GHC使用与编译自身的"Ways"不同的方式编译对象代码时，它实际上就是在做某种"cross-compile", 因此在这个过程中，它无法使用internal interpreter，Template Haskell也没法用了。

GHC的解决方案是弄两份对象代码，一份与自身ABI相容 - 用于internal interpreter，另一份按用户想法来的用于真正的编译结果。这在文件扩展名上会有点不一样(对象文件。接口文件，archive都适用)，比如应用动态链接+profiling的对象文件后缀是`.dyn_p.o`.

问题来了，不光对象代码，接口文件也有两种，可是`HscEnv`的缓存里面并没有区分这两种接口文件的机制 - 都放一块了，此处直接贴上wiki原文：

> The way this is done currently is inherently unsafe, because we use the profiled .hi files with the unprofiled object files, and hope that the two are in sync.

这导致了例如[#15492 — Plugin recompilation check fails when profiling is enabled](https://gitlab.haskell.org/ghc/ghc/-/issues/15492)这样的问题。

> 还有一点问题就是安装到本地的包不一定会提供所有可能组合的对象文件 - 那就得编译了，有时候会导致编译时间和磁盘占用爆炸,所以这个策略也是有着内在创伤性的
>
> [#15394 — GHC doesn’t come with dynamic object files/libraries compiled with profiling](https://gitlab.haskell.org/ghc/ghc/-/issues/15394)

### 3.4.4 -dynamic-too

为了避免编译两种对象代码导致的时间加倍，又一种hack被发明：`-dynamic-too`, 加了这个flag会让GHC表现地像一个多目标编译器并同时输出静态与动态链接的对象文件。

问题是GHC原本没有设计用于这样的用途，在加入`-dynamic-too`之后也没有重新设计过。再贴一次wiki：

> `-dynamic-too` is buggy, slow, and has an ugly implementation

这玩意非常受限，从没有`-profile-too`就能看出来了。

实际实现是用**bug驱动开发**硬凑出来的，想看就看吧

```haskell
-- #8180 - when using TemplateHaskell, switch on -dynamic-too so
-- the linker can correctly load the object files. This isn’t
-- necessary when using -fexternal-interpreter.
dflags1 = if hostIsDynamic && internalInterpreter &&
             not isDynWay && not isProfWay && needsLinker
          then gopt_set lcl_dflags Opt_BuildDynamicToo
          else lcl_dflags
-- #16331 - when no "internal interpreter" is available but we
-- need to process some TemplateHaskell or QuasiQuotes, we
-- automatically turn on -fexternal-interpreter.
dflags2 = if not internalInterpreter && needsLinker
          then gopt_set dflags1 Opt_ExternalInterpreter
          else dflags1
```

### 3.4.5 External interpreter

既然internal interpreter从设计上就缺乏可扩展性，那为什么不搞个外部解释器(iserv)呢. 外部解释器很灵活，只需要一套编译器与解释器之间的通信协议。

然而，在编译器插件这里，出现了一些大麻烦：

+ 有些插件需要访问`HscEnv`,非常难搞序列化
+ GHC的Core表示(有些插件就是在Core上做变换的)是存在回环的，很难序列化
+ 不同ABI带来的诸如Word大小不同之类的问题还是很难搞
+ GHC把全局变量放在运行时里面，所以必须设计一套编译器/解释器运行时之间的同步机制

结果: [#14335 — Plugins don’t work with -fexternal-interpreter](https://gitlab.haskell.org/ghc/ghc/-/issues/14335)

理论上讲让插件单独用内部解释器这事也就完了，但是GHC从设计之初就假设整套体系中只有一个解释器，所以这很不简单。

> 但是论文作者在试着解决它，拭目以待

## 3.5 Plugins and Hooks

[一开始的插件](https://gitlab.haskell.org/ghc/ghc/-/commit/592def09c4f87f517b31aa4c4cec51fc8764a859)只有一种(自定义的Core to Core pass), 后来它逐渐生长分化为各种各样的插件(type-checker, renamer, interface loader, Template Haskell splice
modifier)

但是没有重新设计过！

其结果是，至今还是只有一个`Plugin`类型用于各种各样的插件，用于编译器的各区域，结果是人为地为没有关联的部分增加了依赖关系。

钩子(Hooks)机制是为了适配GHCJS而引入的，用于自定义某些编译器操作。它在[这个](https://gitlab.haskell.org/ghc/ghc/-/commit/6f799899aa7cd9c59c9ebf9c9709f9423d93d307)提交中被引入。

钩子只能通过使用ghc-lib的API来设置，同时这玩意也是放在`DynFlags`里面的，自然下一步就是搞一种很新的修改`DynFlags`的插件用来设置钩子......

## 3.6 Template Haskell

TH不区分编译期使用的模块和运行时使用的模块，所以即使某个模块只是在编译期用了一下，它还是会被链接进最终的二进制产物并拖慢加载速度。

同时，用户没办法让TH用某个包的A版本而GHC用这个包的B版本 - 因为GHC这个单模块环境没法区分。

TH支持执行IO Action, 在使用外部解释器的情况下可能不是特别合适，因为外部解释器可能访问不到所需的文件(e.g. due to sandboxing, remote execution, or execution in a VM)

## 3.7 The Driver

Driver在GHC里用于协调其他编译器/链接器(is responsible for orchestrating other compil-
ers and linkers，这玩意在compiler目录里的名字是main)，后来又被扩展用于支持多模块编译(`--make`)和交互(GHCi), 它的核心类型是`HscEnv`.

> Driver曾经是一个3000行的Perl脚本。

缺乏合适的抽象是主要问题但不是唯一的问题：

**It isn’t independent of GHC-the-program command-line interface**

大多数driver函数所需要的`HscEnv`函数只能通过`GHC.Driver.Main`模块里一个没有文档的函数来获取

```haskell
newHscEnv :: DynFlags -> IO HscEnv
```

**It isn’t self-consistent**

这段没看懂，原文贴在这

> (1) passing a valid DynFlags value is difficult as its “settings” field has to be properly setup. Most users probably rely on initGhcMonad :: GhcMonad m => Maybe FilePath -> m () in the GHC top-level module or duplicate its code to avoid dealing with the GhcMonad abstraction.
>
> (2) the HscEnv created by this function is useless for most purposes because several fields (unit env, interpreter...) have to be properly initialized, which can only be done with setSessionDynFlags ::GhcMonad m => DynFlags -> m () or the similar setProgramDynFlags)also in the GHC module. Or by duplicating their code.

**Documentation is often missing, outdated, or incomplete**

(1) `GHC.Driver.Main`模块在文档中记载为"编译haskell代码的主要API", 然而它根本没法单独使用。

(2) 没什么函数在注释里标记了自己究竟使用了`HscEnv`的哪个字段

**The interface is inherently unsafe**

调整`HscEnv`的唯一方法是去设置`DynFlags`, 但是API用户是真不知道自己该改啥。

**It isn’t full-featured**

一些ghc-lib的客户端(例如：HLS, Haddock)不得不重新实现一个复杂的driver，常常包含大量对原driver的重复。