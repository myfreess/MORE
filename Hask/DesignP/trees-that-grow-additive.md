## 基础泛型

虽然不多，但是有些时候我们的确渴望为这种可扩展的树编写泛型函数。最简单的例子是直接忽略所有扩展字段 -- 但是那个额外的值构造子最好处理一下，不然GHC的Exhaustiveness checker会扔出警告。以一个简单的printer为例

```haskell
printExpr :: (XEXp e -> String) -> ExpX e -> String
```

我们没法预测XExp e对应的具体类型是啥，那就把处理它的责任交给调用者吧() -- 很自然的高阶函数，当然了，可以通过typeclass消除。

```haskell
instance (Show (XLit e), Show (XVar e),
          Show (XAnn e), Show (XApp e),
          Show (XExp e)) =>
    Show (ExpX e) where
-- 我不是粘贴的，坦白说有点烦
```

为了消除大量的重复，引入ConstraintKind扩展。有了它，你可以把typeclass当作类型参数传 -- 灵活是一把双刃剑，用了这玩意的类型体操真真是难懂啊。

```haskell
type ForallX (faiz :: * -> Constraint) e = 
  (faiz (XLit e), faiz (XVar e),
   -- 中间省去很多很多
   faiz (XExp ))
```

现在可以像这样写`Show (ExpX e)`的实例了

```haskell
instance ForallX Show e => Show (ExpX e)
```

还嫌不方便可以自动deriving。

相信已经有人发现了，在不开`UndecideableInstance`时以上代码统统统统别想过编译，所以这是另一个困境，如无必要，勿增Undecideable。

> 

## 参数，真正的参数

在之前的例子里面，并没有用到真正的参数化语法树，不过有些时候我们还是需要它的，例如`let-insertion`这个pass，它需要

```haskell
data ExpX a = ... | Let a (Exp a) (Exp a)
```

也很简单，在树的定义中把参数顺手也传给type family一份即可。(当然它本身也少不了)

```haskell
data Exp e a = ... | AppX (XApp e a) (ExpX e a) (ExpX e a) | ...

type ExpLE a = ExpX LE a
data LE

-- ......

type instance XExp LE a = (a, ExpLE a, ExpLE a)

pattern LetLE :: a -> ExpLE a -> ExpLE a -> ExpLE a
pattern LetLE x m n = ExpX (x, m, n)
```

## 存在类型与GADT

再次默念：GADT只不过是Yoneda lemma的语法糖罢了！

此次的例子为一个简单的EDSL, 此处不多叙述，觉得未尽兴可观看

```
https://www.schoolofhaskell.com/user/edwardk/bound
```

回归正题，这个小小的EDSL包括常量，应用，加法和布尔与运算

```haskell
data Exp c where
  Con :: c -> Exp c
  App :: Exp (a -> b) -> Exp a -> Exp b
  Add :: Exp (Int -> Int -> Int)
  And :: Exp (Bool -> Bool -> Bool)
```

很遗憾，如果不改动这个定义，我们连个简单的printer都没办法写 -- 因为在Apply构造子的类型签名中a是一个局部类型变量，对外不暴露。

那么，可行方案包括

+ 加个printer当参数
+ 加个`Show a`约束

好吧，考虑到除了printer未来大概还会有n多其他需求，做成可扩展的。对于GADT, trees that grow的处理有一条增加事项 -- 局部类型变量也要传给type family，如果选用typeclass解决，那......

```haskell
{-# LANGUAGE ConstraintKind #-}

data Proof faiz a where
  Proof :: faiz a => Proof faiz a

type ExpSh a = ExpX Sh a -- 现在是这个GADT的扩展Exp了
data Sh

-- ......
type instance XApp Sh a b = Proof Show a

pattern AppSh :: () => Show a => ExpSh (a -> b) -> ExpSh a -> ExpSh b
pattern AppSh l m = AppX Proof l m

data Exists f where
  Exists :: f a -> Exists f

-- Exists 是加局部变量用的，没给例子
-- 不知道怎么用
-- 如此傻瓜的我.jpg
```

## 带参数的扩展描述器

参数扩展器也不过是普普通通的代数数据类型罢了

> 等下，Ordinary Algebraic Data Type? 

既然设计模式是从使用中总结出的抽象，例子先行。假设我们要给每个节点加上其源码位置，当然了，可以

+ 给每个节点加新字段

+ 弄个新的值构造子当wrapper

后一种方式较前一种更不精确

好像不太对，前一种方式较后一种更精确

问题：源码位置的类型通常是polymorphic的，允许程序员编写像fmap，fold这样的泛型函数处理。一个非常好的例子是haskell-src-exts。

这里因为需要泛型的是某一组特化的表达式类型(且结构和设计意图基本一致)，直接在树上加参数又不太合适，所以

```haskell
type ExpAn a = ExpX (An a) -- An Annotation
data An a
-- 在扩展描述符上面加参数

type instance XLit (An a) = a
-- ......
type instance XExp (An a) = Void
```

## 层级

即使是使用了trees that grow这种复用AST定义的设计模式，很多时候编译器还是不得不弄好几个AST变体。就单说GHC，前端用HsSyn, Template用一个分离的变体，前端3个主要的pass又有3个变体。这时候有可能会需要层级化的扩展描述符。

```haskell
-- (c :: Component) 这个应该是用了
data GHC (c :: Component)
data Component = Compiler Pass | TH -- TemplateHaskell
data Pass = Parser | Renamer | TypeChecker

-- 可以定义出泛用性强的type family
-- 应该吧，能看出确实省了点代码量

type family PostTC p where
  PostTC TypeChecker = Typ
  PostTC _           = Void

type instance XApp (GHC (TH))         = Void
type instance XApp (GHC (Compiler p)) = PostTC p
```

还是别去管它实际省了几行代码吧，至少结构看起来挺不错()














