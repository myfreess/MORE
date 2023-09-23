# Functor Functors

**原文载于benjamin.pizza**

https://www.benjamin.pizza/posts/2017-12-15-functor-functors.html

俗语云，士别三日，当刮目相看

在范畴论中一件特别有趣的事情是，某一次在某个上下文下了解了某个想法之后，通常很有机会在另一个上下文中重用它。haskell程序员的工具包里绝对不只有Hask范畴，虽然它是最引人注目的那一个。

## 模板：可重用的结构

在Sof和Reddit上这样的问题从来没少过：我有一堆结构上几乎相同的record，怎么轻松加愉快地把它们放在一块料理了？

这听起来有些像那种"OOP课堂上为了讲解继承会出现的虚伪例子"，例如不知道什么痴子才会去用的动物类，鸟类，还有会发出叫声的接口。所以最好还是有个实例，避免大家都觉得在瞎编。

比如一个购物网站中的不同checkout form。

```haskell
data CardType = Visa | AmEx | Mastercard

data Form = Form {
    form_email :: Text,
    form_cardType :: CardType,
    form_cardNumber :: Text,
    form_cardExpiry :: Day
}

data DraftForm = DraftForm {
    draftForm_email :: Maybe Text,
    draftForm_cardType :: Maybe CardType,
    draftForm_cardNumber :: Maybe Text,
    draftForm_cardExpiry :: Maybe Day
}
```

不知道这是啥国家用的信用卡。不难看出来，一个checkout form是个购物信息的小小表格，对于网站上正在填写的草稿每一列都加了个Maybe上下文。

好的，那么等到用户填写完毕之后就该把DraftForm转换成Form了。

```haskell
toForm :: DraftForm -> Maybe Form
toForm (DraftForm
    (Just email)
    (Just cardType)
    (Just cardNumber)
    (Just cardExpiry)) = Just $
        Form email cardType cardNumber cardExpiry
toForm _ = Nothing
```

烦耶，想个什么抽象去掉这一堆玩意吧。

"标准的做法"大概是把Form挖空，让它接受一个类型构造子f作为参数

```haskell
data FormTemplate f = FormTemplate {
    _email :: f Text,
    _cardType :: f CardType,
    _cardNumber :: f Text,
    _cardExpiry :: f Day
}
type Form = FormTemplate Identity
type DraftForm = FormTemplate Maybe
```

而另外一拨人突发奇想，创造了一个编程上的"俗语", 叫设计模式也未尝不可。他们的基本想法是：

为什么不让模板成为一等公民？

即，为什么不把模板当成类型参数？

```haskell
type Record t = t Identity
type Partial t = t Maybe

type Form = Record FormTemplate
type DraftForm = Partial FormTemplate
```

从直觉上看，这是把模板当成了函子的容器 -- 不过是固定大小的，而函子本身也经常被看作值的容器，也许......也许Functor，Traversable，Representable这些抽象可以在这样的模板上复现？

## 自内而来：自函子范畴上的函子

在haskell世界中，范畴通常由**kind**这个概念表示，而形如`c :: k -> k -> *`的类型构造子则概括了kind k下的态射(morphism), 请注意这里是kind签名，不是类型签名。

> https://hackage.haskell.org/package/base-4.16.0.0/docs/Control-Category.html 这是base里面的抽象，希望大家不要拿这个里面的实例为难我

假设我们现在有俩范畴

范畴C的对象从**k1**来，态射向**c**去要。范畴D的对象来源于**k2**，态射的落脚点则是**d**，那么一个很基本的构造呼之欲出了：函子何在？

从C到D的函子应当是个这样的类型构造子

```haskell
f :: k1 -> k2
```

加上一个这样的fmap

```haskell
mapC2D :: c a b -> d (f a) (f b)
```

如果现在说用于表示hask上自函子的Functor class是它的一个特例，应该没人会惊讶。请把k1 k2替换为*，c d替换为(->)

**THEN A MIRACLE OCCURS**.

如果从C到D的函子放在一块，它们又可以构成一个范畴 -- 那在这个新范畴里面函子成了对象，态射就是自然变换啦。所以如果我们从这个范畴出发弄个到任意范畴的函子

> 关于自然变换 https://segmentfault.com/a/1190000012381561

不如就叫它Functor Functor吧！

此时再考虑一下原来的模板类型，毫无疑问是从Hask的自函子范畴到Hask的函子啊

```haskell
-- 函子f g之间的自然变换
type f ~> g = forall x. f x -> g x

-- "functor functors"
class FFunctor f where
    ffmap :: (Functor g, Functor h) => (g ~> h) -> f g -> f h

instance FFunctor FormTemplate where
    ffmap eta (FormTemplate email cardType cardNumber cardExpiry)
        = FormTemplate
            (eta email)
            (eta cardType)
            (eta cardNumber)
            (eta cardExpiry)

-- 自然变换叫eta不知道是不是约定俗成
```

自然FFunctor也要遵守范畴论的基本法对吧，该维持的恒等和结合不能少

```haskell
-- identity
ffmap id = id

-- composition
ffmap (eta . phi) = ffmap eta . ffmap phi
```

现在要写不同Form之间的转换只需要写出其基底函子间的自然变换，然后拿ffmap包装一下就好。而且从Identity到其他玩意还都挺好写 -- 一个generalise函数搞定。

```haskell
generalise :: Applicative f => Identity a -> f a
generalise (Identity x) = pure x

toPartial :: FFunctor t => Record t -> Partial t
toPartial = ffmap generalise
```

## 潇洒走一回：遍历Record

现在我们有了新的强力抽象，应该想的事情当然是 -- 怎么把原来用Functor的一堆组合子搬过来，比如常常被戏称为最终解决方案的traverse，或者它的前驱sequence

```haskell
sequenceA :: Applicative f => t (f a) -> f (t a)
```

光是摆弄符号是不行的，要想掌控抽象而不是被它从巴别塔的废墟上扔下去，我们得清楚地知道抽象的意义。

benjamin对sequenceA函数行为的解读首先涉及一个对Functor抽象的新理解角度：Functor描述的实际上是一种`strategy to produce a value`, 此处不妨就叫它**值分发策略**。

那么sequenceA函数的行为便是：拿到一个装满值分发策略的容器，将其组合为一个能够分发**【装有值的容器】**的策略。

> 别怪我说得拗口，原文是" takes a container of strategies to produce values and sequences them to get a strategy to produce a container of values"

现在的问题是，把上面句子的值换成函子，函子分发策略......应该是个啥？函子可以作为对象，但是函子是没有值对应的啊，没法用常规途径直接塞一个值了事

Li-yao Xia对此问题给出了一个非常棒的解答：https://stackoverflow.com/questions/44187945/what-should-a-higher-order-traversable-class-look-like

他的回答是，对于一个applicative a, 要从它得到一个产出函子g的策略，那是非常的简单：`Compose a g`

```haskell
class FFunctor t => FTraversable t where
    ftraverse :: (Functor f, Functor g, Applicative a)
              => (f ~> Compose a g) -> t f -> a (t g)
    ftraverse eta = fsequence . ffmap eta
    fsequence :: (Functor f, Applicative a)
              => t (Compose a f) -> a (t f)
    fsequence = ftraverse id

ffmapDefault :: (Functor f, Functor g, FTraversable t)
             => (f ~> g) -> t f -> t g
ffmapDefault eta =
    runIdentity . ftraverse (Compose . Identity . eta)

fsequence' :: (FTraversable t, Applicative a) => t a -> a (Record t)
fsequence' = ftraverse (Compose . fmap Identity)
```

需要遵循的规则从Traverable移植一下, 虽然unlawful instance从来都不少，但是这毕竟是参考范畴理论的设计。

```haskell
-- 自然性 
nu . ftraverse eta = ftraverse (Compose . nu . getCompose . eta)
-- for any applicative transformation nu

-- identity
ftraverse (Compose . Identity) = Identity

-- composition
ftraverse (Compose . Compose . fmap (getCompose.phi) . getCompose . eta)
    = Compose . fmap (ftraverse phi) . ftraverse eta
```

traverse的实现看着本来就挺像fmap，只是多了个applicative的上下文，那ftraverse长得像ffmap不奇怪。

```haskell
instance FTraversable FormTemplate where
    ftraverse eta (FormTemplate email cardType cardNumber cardExpiry)
        = FormTemplate <$>
            (getCompose $ eta email) <*>
            (getCompose $ eta cardType) <*>
            (getCompose $ eta cardNumber) <*>
            (getCompose $ eta cardExpiry)
```

原文后半部分还有foldMap，zipWith，Lense和可组合验证的提升版本，有空补上。这个ftraverse属实是一时给我搞不会了。