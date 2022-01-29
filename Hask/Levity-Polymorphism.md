# 从`$`开始的类型过山车

## 起因

新年伊始，假期刚至，正是群友欲上时，聊天群这个舆论战场你不去占领，自然就会有别人占领。一日有群友提到`$`运算符的类型签名，我思来想去，记起它并不平凡 -- 但是未有想法要学习，非常愧疚，感觉手上的手机屏幕也失却了光彩，于是打开Hoogle，搜之，原来是Levity Polymorphism，bing之，寻来一篇paper，下载，后无果。

几天后此事当然已经只剩下那一点念想了，正要放弃的关口，不小心弄崩了自己的win10，又没有做启动盘，只好找了位修电脑的师傅 -- 痛失40，因为是上门服务，年关将近，师傅急于回老家，这样的时候麻烦他，哪里还有脸谈价格，只好照价支付。就是在这百无聊赖之际，为消胸中块垒，我静下心来好好地看起此文 -- 然后度过了一个得机忘失的下午。

(因为偷懒，下文有很多类型签名没写forall)

(写到结尾才发现，polymorphism应该是多态而非泛型，但是我把levity polymorphic写成多变泛化了 -- 多半是精神涣散的结果)

## 背景

参数化多态是现代类型化编程的要道重镇之一，但是对手头紧的旅客来说这过路费好生肉疼。让我们来看看haskell为此付出了什么。

```haskell
bTwice :: Bool -> a -> (a -> a) -> a
bTwice b x f = case b of True -> f (f x)
                         False -> x
```

此函数在a上是泛化的，在haskell里这意味着同一份从bTwice编译出的代码对任意类型的参数x都可用。但是对于习惯和机器打交道的程序员，他们会想到一个令人不安的事实，参数x的类型实际上会影响其调用约定，所以说，当然了，也会影响其编译产物。如果x是一个列表，它应该通过一个放在寄存器中的指针来传递，若x是一个双精度浮点数，那应该通过一个特殊的浮点数寄存器传递，在不同的调用场景下使用相同代码好像不是件简单事。

> 我有点糊涂了，我在看什么？haskell吗？

一个简单而直接的方案是将所有值都表示为指针+堆上的对象，其彼得同样明显，性能马上会降到一个可怕的程度。大多数泛型语言还支持一些未装箱的值，它们不使用指针，举一例，GHC就已经支持未装箱值很多年了(for decades)。

但是这样裸露的抽象用起来总是有些不太舒适，总会有人试着调和一下大量使用泛型和性能之间的矛盾。levity polymorphism被它的作者称为优雅的新调解手段，类型被分入多个kind，每个kind都表示了一种内存布局，kind在此决定了函数的调用约定。它不会让现有的代码更快，它只是让有经验的haskeller能更愉快地写高性能代码，同时又尽可能加强其可重用性。在GHC 8.0.1中，levity polymorphism被首次投入使用。

不过，为了明晰其出现的背景，还需要就haskell多说几句。

看看这个循环`[1]`，它计算整数1到n的和

```haskell
sumTo :: Int -> Int -> Int
sumTo acc 0 = acc
sumTo acc n = sumTo (acc + n) (n - 1)
-- 问就是partial function + space leak + loop三连
```

`[1]`不知道作者是冷幽默还是认真的，看了下，第一作者是写了Dependent Types in Haskell的Richard A . Eisenberg, 然后是SPJ，纯度应该是杠杠的

haskell中的Int类型是包装过的，任意一个值被表示为一个指针，指向在堆上分配的某个两个机器字长的内存单元。等价的C程序用不着什么指针，也不必反复地申请内存存放thunk，只需一个三条机器指令的循环，也没有繁重的内存周转。GHC倒没有阻止用户做优化，只需要把类型定义啥的改改就行。

```haskell
sumTo# :: Int# -> Int# -> Int#
sumTo# acc 0# = acc
sumTo# acc n = sumTo (acc +# n) (n -# 1#)
```

后缀`#`的用途就是标记这是未装箱的值，或者与未装箱值有关的运算。看起来差不多，编译出来千差万别。GHC并非对Int相关运算不做优化，它有个strictness analyzer，还有些其他优化手段，常常可以把sumTo优化成sumTo#，但是不保证总能做到，所以在乎性能的人还是会自己动手。

像Int#这样的未装箱类型还有很多，Char#, Double#, 给出未装箱类型，则对应的装箱类型很容易自行定义，实际上，标准库中的Int就是

```haskell
data Int = I# Int#
```

对于装箱类型，它们内部仍可细分为提升类型(lifted type)和未提升类型！提升类型是惰性的，那意味着它有个表示不停机运算的值。

> 而未提升类型诸如ByteArray#实在是太默默无闻，我完全没了解过。

未装箱元组(Unboxed Tuple)地位稍微有点特别，它不存在于运行时，举一例

```haskell
divMod :: Int -> Int -> (# Int, Int #)
```

实际上编译完之后会变成一个多返回值的函数，其返回值存储于多个分离的寄存器中。

现代版本的GHC允许将未装箱元组作为函数参数 -- 编译期直接处理成多参数。

一个有趣的地方是，未装箱元组的嵌套层次和计算无关。就是说，虽然`(#Int, (# Float#, Bool #) #)` 和 `(# Int, Float#, Bool #)`是完全不同的类型，它们在运行时倒是别无二致。

现在要做的是就是，把未装箱类型和泛型拉到一起来考虑

再看一遍bTwice的定义。

```haskell
bTwice :: Bool -> a -> (a -> a) -> a
bTwice b x f = case b of True -> f (f x)
                         False x
```

不支持`x :: Int#`或者`x :: Float#`这样的实例看起来天经地义，但是对一个已装箱未提升的类型，例如`ByteArray#`，它也不支持(因为函数默认应该是call by need，即lazy的，必须是已提升类型)。

> 奇怪的是不支持(# Int，Int #)，这不是内存布局确定的吗？也许是工程上有什么麻烦

**实例化原则(The Instantiation Principle)**：不可使用未提升类型实例化类型变量(GHC限定)

同样的问题Java用户也见过，在他们那里，"未提升"要换成"未装箱"。Java为此提供了一些特化的标准库容器(eg. IntStream)。

GHC如何实现此原则？换言之，如何判断一个类型是不是已提升的？也许有些人会认为，未提升类型有限，那么为它们写些条件判断就好。但是haskell有继承自System F-omega的抽象 -- 可用于对类型进行分类的`kind`。

举例

```haskell
Bool, Int :: * -- 读作Type
Maybe :: * -> *
(->) :: * -> * -> * -- 后面的箭头和前面的不是一个意思，前面的是函数类型的类型构造子，后面的只是对其kind的描述
```

已提升类型全体归于`*`, 而未提升类型以`#`为纲领

```haskell
Int# :: #
Float# :: #
```

那么bTwice的类型签名究竟该是啥呢

```haskell
bTwice :: (a :: *) . Bool -> a -> (a -> a) -> a
```

不过，以上设计尚欠考虑，以之前的sumTo#为例, 仔细一想，它的类型是`Int# -> Int# -> Int#`,但是(->)的kind签名是

```haskell
(->) :: * -> * -> *
```

这在kind层面上是说不通的(ill-kinded)！

GHC当然想了些办法 -- 但是不好说这点子合适。过去的许多年里，GHC靠的是所谓的sub-kinding解决此问题。

```
     OpenKind
    /        \
   *          #
```

然后把(->)的kind设计成了这样

```haskell
(->) :: OpenKind -> OpenKind -> *
-- 标有OpenKind的地方可任意用 * 或 # 替换
```

现在，稍微回想一下，类型推导+泛型+subtyping一锅炖一般会结合出什么呢？

> 哦，是麻烦++! 

因为subkinding的引入，GHC的类型推导实现不得不贴上各种尴尬，无原则的特例(special case)作为补丁。麻烦并非到此为止，为了尽量避免把OpenKind暴露给用户，GHC又规定只有完全饱和应用(即得到两个类型参数时)的(->)才有上面那个奇怪的kind，对于偏应用，就用

```haskell
* -> * -> *
```

来骗骗用户。

一个糟糕的抽象，加上一个部分掩盖糟糕本质的诡计，Haskeller们很快在上面饱受折磨，叫苦连天。一批批类型论新手周期性地涌向邮件组询问为什么(->)的kind是`* -> * -> *`而GHC能够接受`Int# -> Double#`这样的类型，OpenKind的名号如小丑般在报错信息中猛地翻个跟头，嘲弄着正为编译不过而头大如斗的用户。

> 比较怀疑他们是怀着高兴的心情去邮件组的，年轻人的第一次bug report？

kind polymorphism的引入让一切变得更糟，而紧随其后的kind equalities几乎要毁灭这一切。

在地震面前显然漏水算不得啥了，但是还是来看看另一个使用OpenKind的玩意出现了什么麻烦。看看这个函数

```haskell
f :: Int# -> Int#
f n = if n <# 0# then error "Negative argument"
                 else n /# 2#
```

此处的`error :: String -> a`作用是打印出错误信息然后使程序运行停止。从实例化原则来看，此函数显然问题多多，因为a被实例化为`INT#`，但是在此处打破原则才是合理的 -- 因为error并没有真正去操作任何类型为a的值啊，它只是执行副作用。所以GHC又开始大开方便之门，魔法来了

```haskell
error :: (a :: OpenKind) . String -> a
```

问题在于，这该死的魔法就像四维碎片一样脆弱，稍瞬即逝。仅仅是试图自己封装一下也会毁掉它

```haskell
myError :: String -> a
myError s = error ("Programm error" ++ s)
```

由于GHC将error的类型根据上下文推导为`forall (a :: Type) . String -> a`, 魔法如古老者的文明一般失落了。

## 泛型，而非子类型

在GHC 8.2.1中实现了一个内置的类型级别常量`TYPE`

```haskell
TYPE :: Rep -> Type
-- 此处的Type就是*
```

Rep和Type的定义如下

```haskell
data Rep = LiftedRep
         | UnliftedRep
         | IntRep
         | FloatRep
         | DoubleRep
         | TupleRep [Rep]
         | .... etc....

type Type = TYPE LiftedRep
```

Rep类型是一组用于在编译期标记某个类型的内存布局的标签，通过TYPE携带。它不是魔法，就是个`perfectly ordinary algebraic data type`，通过**DataKinds**扩展提升到kind层次，Type也就是个普普通通的类型别名。现在我们可以按照内存布局给类型分类了

```haskell
Int         :: Type
Int         :: TYPE LiftedRep -- 展开Type
Int#        :: TYPE IntRep
Float#      :: TYPE FloatRep
(Int, Bool) :: Type
Maybe       :: Type -> Type
```

+ LiftedRep <=> 指向已提升值的指针
+ UnliftedRep <=> 一个指向未提升值的指针
+ IntRep <=> 无封装整数
+ DoubleRep，FloatRep ......

不过，未装箱元组可能会引起一点疑惑，比如[Rep]是个什么。

其实从例子来看一目了然。

```haskell
(# Int, Bool#) :: TYPE (TupleRep '[LiftedRep LiftedRep])
-- ' 是一个标记
-- 提醒代码阅读者此处是一个类型层面的列表
(# Int#, Bool #) :: TYPE (TupleRep '[IntRep, LiftedRep]) -- 非常合理，Int#对应IntRep
(# #) :: TYPE (TupleRep [])
```

不过，现在的设计需要处理元组嵌套层次的问题

```haskell
(# Int, (# Bool, Double #) #)

(# (# Char, String #), Int #)
```

上例中两个类型的运行时表示完全一致，但是kind不一样，如果选择把kind压平，那么会在kind的相等性理论上惹麻烦。好在现在没发现什么合理的用例证明这种压平处理有价值，那么还是使用更简单的设计吧。

## (->)重现

haskeller可以安心了

```haskell
(->) :: (r1 :: Rep) (r2 :: Rep) . TYPE r1 -> TYPE r2 -> Type

error :: (r :: Rep) (a :: TYPE r) . String -> a
```

这样的类型在`r :: Rep`上是泛化的，那么其实最合适的名字是**representation polymorphism**，但是设计者希望它可以轻松地用搜索引擎找到相关资料 -- 其实是让名字更短更易识别，于是最终命名为**levity polymorphism**。

## 溯洄从之

在上面引入的设计中，`TYPE`的类型是`Rep -> Type`,而Type是`TYPE LiftedRep`的简写，所以`TYPE`的kind和TYPE自身有关，这能行吗？

答案是，和其他为universe分层的denpendently-typed语言不一样，GHC的选择是这一条公理

```haskell
Type :: Type
```

这个选择让语言在逻辑上看起来非常地不一致，但是没有危及类型安全。

选择`TYPE :: Rep -> Type`的理由是渴望在未来可以通过增加扩展是haskell成为一个完整的dependently-typed语言，所有的类型将与值获得相同的地位并且可在运行时传递`[1]`。

`[1]`:原文说大概会是一个指向描述类型的语法树的指针

## 限制

不加限制的levity polymorphism无法编译，回归最初的例子看一看

```haskell
bTwice :: (r :: Rep) (a :: TYPE r) . Bool -> a -> (a -> a) -> a
```

直接编译成native不现实，运行时/编译期为bTwice生成特化版本呢？有些希望，但是带来了显著的工程技术上的挑战，即使是在一个基于jit的系统中，还是来看看应该做哪些限制吧。

+ Disallow levity-polymorphic binders，对任意与值绑定的变量，应该给定一个具体的kind

+ Disallow levity-polymorpic function arguments，参数通过寄存器传递给函数，必须在编译期就能知道要使用多大的寄存器。

因为牵扯到kind的类型推导太难做，GHC就没考虑推导levity polymorphism。有需要只能自己写

```haskell
myError :: (r :: Rep) (a :: TYPE r) . String -> a
myError s = error ("Program error" ++ s)
```

## 应用

### 关于未提升类型

在实现levity polymorphism之前, GHC一直用苛政重法治理未提升类型:

+ type family禁止返回未提升类型. 

在之前GHC使用的kind只有*和#，所以这样的代码在kind层面是正确的

```haskell
type family F a :: # where
  F Int  = Int#
  F Char = Char#
```

然而，GHC实际上没办法编译像`f :: F a -> a`这样的代码，因为其参数究竟该用多大的寄存器在编译期根本没法得知。Int#和Char#有着完全不同的调用约定，未装箱元组的kind也是#，更糟心了。

+ 未提升类型禁止作为索引使用

不可以将未提升类型传递给type family或者将它作为GADT的一个索引(index). 回望过去，这似乎是一条多余的规则。但是当时关于未提升类型的理论研究尚未充分到能保证这样做是安全的，为了安全起见，最好还是先保守些。

+ 未提升类型必须完全饱和

GHc中有数个参数化的未提升类型，如`Array :: Type -> #`,想象一下，我们有一个kind限制为`Type -> #`的类型变量，现在要用Array去实例化它，然而，由于#过度宽容并包的定义，这样的kind限制实际上没法确定具体的调用约定，任何这样的抽象都会招来麻烦。例如，`(# , #) Bool`(这是一个偏应用的未装箱元组类型)的kind也是`Type -> #`,它的调用约定和`Array#`毫无相像之处。

在重整规则之后，这些限制变得明确了许多，F这个type family的定义现在是kind层面不合理的(ill-kinded), 对偏应用的未提升类型进行抽象是安全的，因为我们可以通过更精细的kind确定其调用约定。

### 多变泛化函数

如今的`$`是这样的

```haskell
($) :: (r :: Rep) (a :: Type) (b :: TYPE r) . (a -> b) -> a -> b
```

实际上泛化版本的($)在GHC里存在有一段时间了(因为有用户请求了)，但是是用类型系统的特例实现的。

> 在ghc-prim-0.6.1的GHC.Types模块中，Rep类型实际上的名字是RuntimeRep，同时它还支持不少花样，例如SIMD向量，未装箱和类型，|Word/Int|[8/16/32/64],指针(供FFI使用)，这也证明levity polymorphism确实可扩展性行

我们只能泛化函数返回值。

### 多变泛化类型类

一直用+#(Int#)和+##(Double#)确实给人一种很OCaml的感觉，现在让Num来搞定一切吧。

```haskell
class Num (a :: TYPE r) where
  (+) :: a -> a -> a
  abs :: a -> a
```

typeclass在haskell中通过所谓的字典(directionary)实现, 每个typeclass实例在运行时表现为一个字典，里面装着相应的值和函数，有typeclass前置约束的函数(不是typeclass里面定义的)在编译期直接增加参数把字典传进去。所以此处的+实际调用时仍然是单态的，不用担心。

不幸的是，虽然一切似乎都能用，但我们已经踩在悬崖边缘，**Two Steps From Hell**。

```haskell
abs1, abs2 :: (r :: Rep) (a :: TYPE r) Num a => a -> a
abs1 = abs
abs2 x = abs x
```

abs1可通过编译，但是abs2不行。abs2显然只是abs1的eta展开版本，但是由于currying，它们的编译产物会有非常大的不同。

在haskell中，一个编译过的函数会带有一个叫做元数(arity)的标签，此标签标记了通过机器的常规调用约定可对此函数传递的参数数量。

对于abs1，它的元数是1，要传递的是Num a实例所对应的字典，它返回的则是一个单参数函数的内存地址。

abs2的元数是2，更高的元数使它具有多变泛化的参数，同时阻止了编译。

在进行编译时，eta等价并不等价。

## 试验

在levity polymorphism作为一种新技术广泛应用于GHC标准库时，开发者们的态度较谨慎 -- 他们希望此特性有足够多的实战测试。首先6个`[2]`以前作为特例提供了处理未提升类型能力的函数被改写为多变泛化版本，即使是这个改动也并非毫无争议：数个用户非常大声地(或者正确地)抱怨在GHCi中看到的($)签名过分复杂。处理方案是在pretty print时默认所有kind为Rep的类型变量为LiftedRep，除非用户使用`-fprint-explicit-runtime-reps`这个flag。

`[2]`:分别是`error, errorWithoutStackTrace, undefined, oneShot, runRW#, ($)`

因为(.)之前没有用特例泛化，当时也就没有改动(当然现在已经改了)。

与函数泛化工作的缺乏相比，在GHC的base和ghc-prim包中的76个typeclass中，足有34个可以用levity polymorphism改写。

对levity polymorphism的支持与TypeInType扩展的开发近乎同时完成，它们互相补全了对方。但因此也很难指出levity polymorphism对GHC单独造成了什么影响。不过呢，在真正工业应用前还是找出了一些陷阱

+ 管理未装箱元组被证明是个精细活。未装箱元组的签名现在变成了`(# , #) :: r1 r2 (a :: TYPE r1) (b :: TYPE r2) . a -> b -> (# a, b #)`,参数数量从2变成了3`[*]`，但是普通元组没变还是2，在编译器的多个工作场景下(eg. 序列化/反序列化支持分离编译所需的接口文件)，必须小心地判断应该乘以/除以2还是3(取决于元组是否装箱)

`[*]`:我糊涂了，可能是DataKind扩展的需要。

+ 判断类型是否提升也很困难。不可能判断一个levity polymorphic的类型该是lazy还是strict。相关bug报告就跟正弦波一样稳定输出，不断地有未提升类型在各种意想不到的场合出现。屋漏偏逢连夜雨，在类型推导时GHC要用可变引用(mutable cell)追踪type/kind，但是**检查levity的函数是纯的**。它没办法访问可变引用，为此不得不在检查类型的levity之前将填充满的可变引用替换为其内容(GHC管这个过程叫zonking)

> zonking是荷兰语中的"晒太阳"，对应此处把内容从可变引用里拖出来好像也挺贴切

与此相关的挑战是GHC8.2到现在都不支持在类型表示(type representation)上的计算(通过type family)，当code generator需要确定在函数应用上该用啥调用约定时，type family会使它陷入迷惑。纯粹的工程问题，但是看起来要干净利落的解决很考验技巧。

+ eta展开也变得像拆弹一样难整。糟糕的是，GHC的primitive必须被eta展开，避无可避。

+ 检查无法处理的levity polymorphism是件脏活。理想情况下type checker应该及时回应任何不当的类型错误，尴尬的是，只有在类型检查结束所有类型变量都已经合一之后才能开始检查levity polymorphism的问题，类型信息的脱糖使得在这一阶段提供可读性强的错误信息非常困难，即使此pass追踪的代码上下文比type checker简单许多，这需要更多工程上的努力。同时在GHC的Core language中函数调用很容易识别，但是报错时需要以表层含糖的语法(sugary surface syntax)来做报告, 这就需要识别此函数调用对应的表层构造范围。

## 相关工作

levity polymorphism可以用于其他语言吗？这是来自设计者的回答：

> Our answer is, emphatically, **yes**

不过就目前看来，其他语言各有其解决之道

### 统一表示

OCaml用一个机器字表示指针 || 未装箱整数，区分俩者的方式是偷了一个bit当作flag。这个方案的可扩展性不是很行，而且拖慢了基础算术(因为有额外的位检查)。在那些对浮点数操作使用不同register bank的机器上这一方案显得有些尴尬，对于那些一个字长装不下的类型它没法整合进去，比如双精度浮点数，未装箱元组，基于现代处理器SSE指令集的多值返回。

Java的方案则放弃了值类型，它们与泛型无缘。

### 编译期单态化

C++，MLton这个Standard ML编译器和Rust使用此方案。单态化回避了很多问题，但是不太适合haskell的语言特性

+ 多态性递归(Polymorphic recursion), 这玩意就没法静态地单态化。

+ 高秩类型(Higher rank types), 举例说明，如
  ```haskell
  f :: (forall a . a -> a -> a) -> Int -> Int
  ```
  f的参数必须是多态的，因为f可能会用各种极度不同的类型去调用它

关于多态性递归，此处有一个从Stephen Diehl的What I Wish I Knew When Learning Haskell(version 2.5)借来的例子

```haskell
data Tree a = Leaf | Bin a (Tree (a, a))

size :: Tree a -> Int
-- 类型推导在这里不太行得通
size Leaf = 0
size (Bin _ t) = 1 + 2 * size t
```

这些语言特性并非什么奇技淫巧，一些被广泛使用的库中比比皆是(例如lens)。

单态化所需的调用追踪会影响到分离编译和模块化，C++编译器需要在链接器中删除重复数据，MLton需要整个程序编译。

### 运行时单态化

C#/.NET的实现使用了一个JIT编译器进行特化，编译并且运行多态函数，特化版本的生成是惰性的。而且若类型是相容的就直接分享代码(大概引用类型都用同一份？)。看起来.NET是唯一支持在未装箱类型上的多态的流行语言实现。

TIL编译器(好像是个学术研究用的编译器)使用的则是**intensional polymorphism**和积极的优化进行单态化，但是也允许编译多态函数，然后在运行时做类型检查。

这种方法禁止类型擦除，运行时的类型操作难免会带来一点性能上的损失，但是从C#/.NET的表现来看，只要钱给够设计好其实性能还是很不错的？(误

## 追踪想法的源流

levity polymorphism使用的kind系统可看作对TALT和Cyclone中部分想法的泛化。

TALT是一个类型化的汇编语言，在它的类型系统中每个kind描述此kind下类型值的大小，kind `T^i` 下的类型值的大小占i个字节。它的类型系统甚至支持类型多态，还能在运行时传递i的值，需要知道类型大小的操作(例如用索引访问数组)需要类型具有某个固定的kind`T^i`。对于kind为T(即大小被擦除)的类型则上述操作不能使用，有点像levity-polymorphic的类型。

Cyclone则是一个类型安全的C方言，建构在TALT的成功基础上。Cyclone支持两个kind，A(代表"any")和B("代表boxed")，它使用sub-kinding，B可以转换成A。在此系统中，任意指针类型的kind是B，全体类型的kind都可以是A。

> Cyclone现在已经不再发展，其研究成果大多数已经应用于Rust语言。

## 优化

优化这块Scala对手工优化的余地留得比较多。

## 求值顺序

不必担心，一切如常。**there are clear parallels between levity quantification and evaluation-order quantification.**

## 读后感

连类型系统都要考虑计组，早该学学了。


























