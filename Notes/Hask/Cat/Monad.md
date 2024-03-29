
约一年前(2020年初)我看到了这两篇blog：

https://two-wrongs.com/the-what-are-monads-fallacy

https://byorgey.wordpress.com/2009/01/12/abstraction-intuition-and-the-monad-tutorial-fallacy/

它们意在劈破缠绕于Monad这一概念之上的都市传说与误解，为想要学习Monad的人指出一条清晰的独木桥。

第一篇文章提出不可像知乎键政圈那样搞一些所谓“以小见大”的把戏，举某个单例然后声称“这就是Monad”。的确，在blog里扳着手指头像学前班小朋友那样数“我会用的Monad有IO，Maybe，Reader，Writer......”，只消一眼就能让人自然而然地产生不信赖感，这样的blog对于自抬身价毫无意义。相反，学究气地陈述Monad的来历，Monad的定义(?)再加上一点点范畴论以及从haskell抄来的几条规则，最后再以Js或Java代码编写的一个精心选择的实例收尾，证明Monad使代码变得简洁了，多么地可信啊! 

唯一的问题就是，为了降低学习成本，很少有人把范畴论部分讲对了，而代码部分充分发挥了上文中提到的以小见大的技巧，举了一个Maybe，State的例子，就敢放心大胆地说这“就是”Monad。说得神棍气一点，就是“我已习得Monad之抽象本质”, 也许把本质换成大道能更加凸显出他们在计算机神学方面的惊人成就。还好他们的读者一般不用真的去写haskell。从这点上说，也许他们的做法是对的。

但是如果你想实际使用Monad,那么不要恐慌焦虑，看看这个：

+ Monad是一个庞大家族，它有很多成员，要用什么Effect就去学相应的Monad。

+ 已经学习过的Monad们「可能」对学习一个新的Monad毫无帮助，比如学过MaybeMonad并不能加速理解ContinuationMonad，说不定学个Scheme帮助还更大一点。但是如果你学过Reader，Writer了，那么StateMonad你多半会理解得很快。

+ 即使是阅读并理解了正确的Monad的定义(范畴论或编程中)，那也对实际运用Monad能力的增长没啥意义,范畴论本来就有个别名叫“抽象废话”!

第二篇文章诙谐地提出存在于Monad教程中的不恰当的比喻对学习Monad有害。举个栗子，Joe是一个haskeller，在学Monad，他花了数周时间，然后突然之间，他悟了，大彻大悟! 之前接触的实例突然间升入一个高层次的抽象殿堂之中，熠熠生辉!他想, 原来Monad就像一个卷起来的煎饼,而从伴随产生的Monad饼皮无限薄!

然后有个新人来问他，“如何学习Monad?”。Joe当时就来劲了。“非常简单”, 他说，“只要想象一张无限延伸的煎饼，Monad就是那样的东西!”显然他的热情把又一个可怜人带到迷惑的境地, 那个新人的路要难走了。

> 如果你有幸掌握了函数式编程或者精通Monad，求求你别告诉我  -- 何幻

