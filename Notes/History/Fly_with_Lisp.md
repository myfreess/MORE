# 与Lisp肩并肩螺旋升天

Ada，这古老而朴拙的语言时至今日仍有人追捧。大多数未曾学习过它的人大概只了解过欧洲航天局的Ariane 5号火箭事故，1996年6月4日，它的初航仅仅过了37秒就玩完了。但是，事故原因是浮点数转16位整型时发生溢出，从语言特性来看，Ada的安全保障很详尽，只是它没有变得成功而已。

成功真的是非常棒的遮羞布啊，C也变成高性能的代表了，js也有人以精通为荣了，C++也类型安全了，哈哈。

我从Practical Common Lisp一书上见过一个类似的故事，这次的主角是Harlequin CommonLisp的一个移植版本，一架叫做DeepSpace-1(深空一号)的探测器，还有这台探测器上的自动控制系统**Remote Agent(RA)**。

关于深空一号，唯一需要在这里指出的是它是单程的，现在(2022.1.29)正在绕太阳转，在之前它用了几年时间完成一些科考任务。

1999年5月18号凌晨7点左右，位于地面的开发组发现RA好像没按预期的那样执行任务，在上次(1999.5.17, 11:04 am PDT)回传的数据包里面还表示一切如常呢。经检查，是RA Executive -- 三个子系统中负责直接执行任务的那一个由于一个非常罕见的race condition阻塞了，不幸中的万幸是这三个子系统里面还有一个是负责恢复(recovery)的，它回传了所能获取到的最多信息。

```
Debugging a program running on a $100M piece of hardware that is 100 million miles away is an interesting experience. 

> https://flownet.com/gat/jpl-lisp.html

这篇文章的作者Ron Garret后来去Google弄了个talk，标题是The Remote Agent Experiment ：Debugging Code from 250 million miles away
```

一个远在10亿英里之外，价值10亿的设备急需自己去debug，这样的一杯早茶未免太提神了。考虑到事发时间，可能当时开发组人员大多还在梦乡之中，也可能他们是轮班看管。不过有件事很清楚，如果当时他们屈从了无情的政治压力，选择用C++开发，那么还在睡觉的人就不必叫起来 -- 没救了。但是他们选择了Lisp，所以他们连接到DS-1上的Common Lisp REPL，淡定地开始debug，然后解决了问题(但是没修复这个bug)。

> invaluable adj. 极有用的，无价的

但是故事的结局是，DS-1的成功经验半点没影响到结果，SHARP和Plan-AI为Lisp在JPL实验室的征程献上了最后的挽歌，Lisp的时代没落了。

> 该开发历程留下了一篇论文：Formal Analysis of The Remote Agent Before and After Flight，不过没lisp什么事了

## 背景

20世纪90年代，NASA开始筹备所谓的"新盛世计划"，该计划的指导思想是**better，faster，and cheaper**,主要目的是应对未来可预见的经费缩减。 但是从后续表现来看，显然决策者的心思全放在cheaper上了。这个指导思想是时代的产物，cheaper也是冷冽的自嘲，不是他们想省钱，是没经费。毕竟好像有某位名人说过，20世纪是生物的世纪，也不是航天的啊。很可惜，因为Lisp的不流行，它没赶上民营航天的热潮，该死的JavaScript，该死的马一龙。

言归正传，消减成本的一大好方法是第一次就把事做对，在软件开发上什么能帮助程序员把事做对？静态类型？TDD？当然是有效的，但是对于航天业而言还不够。像阿波罗登月计划那样在发射三天前才发现月球重力方向算反了，或者更悲剧一点，因为单位搞错永远地失去了Mars Climate Orbiter(JPL出品，搞出这个乌龙的老哥叫Lockheed Martin), 这种事对航天事业的打击真的太沉重了。新盛世计划在第一次任务中的选择是：大力发展Model Checking技术

## SPIN

什么是SPIN？ SPIN是一个分析有限状态并发系统正确性的工具。1997年，RA处于开发阶段时，开发组非常有先见之明地引入了它，并且对RA Executive 系统核心服务的一组子集进行了建模和验证,这一着棋揭开了系统中数个并发bug的面纱。

SPIN有一个自己的语言，叫PROMELA。一个PROMELA程序包含一组顺序执行的进程，它们通过带缓冲区的channel在彼此之间进行消息传递，不过仍然有共享的变量。顺带一提这语言还有macro，挺黑色幽默。原系统期望得到的性质会被写成PROMELA程序里面的断言语句，而每一个断言语句又对应一个线性时序逻辑(Linear Temporal Logic, LTL)的公式

每个PROMELA进程都对应了一个有限自动机，而整个系统的全局行为通过计算它们的状态转移置换获取(一次转移视为单步指令，然后打乱执行顺序，生成一个非常大的状态空间，但是这个状态空间绝对有限)。执行模型检查时，SPIN将LTL公式翻译成Buchi自动机, 在一番说起来很乏味笔者也不会的计算之后会得到一个新Buchi自动机，这就可以判断目标系统是符合要求的了。(不是包证对的意思)

> 如今如果想玩model checker，可以试试Alloy

## Remote Agent

新盛世计划的前置背景则是，航天级别的处理器能力较以前是强多了，支撑得起更复杂的新世代航天软件，这才有了RA的横空出世。

RA的主要组件包括:

+ The Planner and Sheduler(PS), 它负责从给定必须发生的基础事件生成执行方案。简单来说，统筹全局。

+ The Smart Executive(EXec)， 它从Planner/Sheduler处接受任务序列并执行，同时还要为并发任务提供调度，提供类似OS的功能。

+ The Mode Identification and Recovery component(MIR), 又叫`LivingStone`,负责监控错误和向Executive发送错误情况下的recovery action。

在发射前的那次验证开发组选择了RA Executive，不为啥，它管着并发呢，不过这么说还是太粗略了，实际上它是一个多线程操作系统和一个基于子目标的AI语言`[1]`的结合体。

`[1]`: 古典AI语言，如果想了解一下，可以试试Prolog。

在概念上，RA EXEC有三层，听起来有点像煎饼或是蛋糕。一组核心服务负责执行并发任务(调度)，一组引擎模块负责执行计划(系统调用？)，剩下的一组是科考任务所需的任务程序。它会持续监控系统各资源的状态是否符合预设的一组性质，一但哪个被打破就执行recovery。嗯，它也是lisp特征非常浓厚的一个程序，使用一组被称为`Executive Sequence language`的lisp宏编写。

## That Bug

1999年的那个死锁并没有当时马上就打补丁 -- 毕竟条件真的很难碰到，起飞之前JPL做的300小时模拟测试就没碰到过，而且1999年5月时DS-1都飞了有半年了才突发恶疾。事后原开发组在19日定位到bug，然后他们写了文档，避开知道bug细节的人做了clean room，用SPIN成功找到错误所在处。有点让人哭笑不得的是，这个bug和前期验证找出的一个并发bug完全同构。

```lisp
(loop
  (when
    (or (/= count (esl::event-count event1)) ；A
        (warp-safe (wait-for-event event1))) ；B
    (setf count (esl::event-count event1))
    ; ...
    (signal-event event2))) ；C
```

虽说没什么人接触过他们的codebase，学习CommonLisp的人也越来越少，但是对于熟悉多线程程序的人来说这玩意应该好懂(首先笔者是不熟的啊)。when语句的条件是一个序列式的or语句`[2]`，用中文说出来便是：如果事件计数器没有改变，那就等待，否则执行when语句的body。这样的语义大概是为了避免干等着事件队列，但是如果一个事件在A和B中间出现了，烷基八氮了，笑死，根本不处理，直接睡觉。当前进程睡大觉了，但是产出事件的另一个进程还在等着C处的事件叫醒自己，于是，在距离地球10亿英里的深空探测器上，一场极限拉扯开始了......

`[2]`: or的第二个参数实际上是需要执行的副作用，且严重依赖于求值顺序，所以`(or b1 b2)`实际上的语义是`if b1 then true else b2`。

## 事后

他们搞了个把PROMELA翻译成Java的工具(Java PathFinder)，因为Java好招人。虽说如此，DS-1上运行的还是CommonLisp，都发射了也没法换啊。

## 随想

大家还是和平相处吧，笔者我啊，真的要学Clojure了

