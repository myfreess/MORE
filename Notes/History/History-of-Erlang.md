# 文本观察/A History of Erlang

## 简介

joe老爷子说细节主要来自*Concurrent Functional Programming for Telecommunications: A Case Study for Technology Introduction*这篇论文，毕竟时间日期之类的东西人很难记清楚。

## 概念阶段-1985及以前

### 问题背景与舞台

1974 - 电话交换机AXE, AXE专用编程语言PLEX

1988 - 新型号交换机AXE-N

PLEX已经具有热更新功能了，然而，PLEX是一门使用指针和链表管理内存的语言。在参考了PLEX的教训后，Erlang使用了垃圾回收机制。

在它们出生的年代，语言级别的进程概念还是个新鲜玩意(只有Ada, Chill，Eucild几个上古语言有)，由于它们都提供语言级别的内存管理和进程隔离措施，基底操作系统提供的大多数服务它们是用不到的。其他一些系统服务，例如资源分配或者设备驱动之类用来访问硬件的东西，可以用C编写后动态链接进Erlang的运行时系统。PLEX已经挂了，不需要考虑这些问题。

在Joe老爷子刚进爱立信时，同实验室的Mike Williams向他讲述(hammered into his brain)了并发语言的三条中心性质：创建进程的耗时，两个不同进程间做上下文切换的耗时，以及进程间消息传递的耗时(the time to copy a message between two process)

PLEX最后的遗产是错误隔离的思想，进程/硬件崩了只应该影响到直接相关的事务，其他进程/机器应该像没任何事发生一样。一个直接结果是Erlang为了避免进程间的悬垂指针把消息传递机制设计成了复制消息数据，没有内存池之类的共享机制。由于erlang是为电话交换机编程所设计的(多台位置接近传输可靠的机器实现单个服务)，这样做可以避免某台硬件因事故下线后整系统罢工。

### 需求、需求、需求......

为了探索适合电话机编程的语言该是什么样，实验室选择使用各种各样的语言编写一遍电话服务**POTS**(*Plain Old Telephone Service*)，然后比较其结果，这个项目被称为**SPOTS**(*SPC for POTS*), 后来项目改名为**DOTS**(*Distributed SPOTS*), 最后的名字是**LOTS**(*Lots of DOTS*)

### SPOTS, DOTS, LOTS

1985年Joe加入实验室时SPOTS项目已经结束，DOTS项目刚刚开始。Joe向他的老板提问该做些什么，而他的老板只告诉他"解决爱立信的软件问题"。

Joe和他的实验室同僚是整个公司第一批试用Unix的人(跑在VAX上)，他们尝试了Ada, Concurrent Euclid, PFL, LPL0, Frames和CLU。

SPOTS项目的论文给DOTS留下了这样几条指引：

+ 需要一个小语言，大语言实现和工程师培训上都有困难。
+ 函数式编程不错，但是不能只有函数式，免得把交换机数据库当成函数参数到处传
+ 逻辑式编程非常好
+ 问题的核心是并发，但结合以上几点，将并发加进一个基于规则的声明式语言和基于对象的系统还没有人研究过。

在那时，他们尝试过的声明式语言只有PFL和LPL0, PFL是一个扩展过的ML方言，LPL0则是一个基于Haridi's natrual deduction的逻辑式语言。

在之后的LOTS项目中Joe接触到了smalltalk，然后就是prolog。

## Erlang诞生-1985到1988

### 早期试验

Joe最早使用smalltalk编写简单的电话模拟程序，思路大概是这样：

+ 电话和交换机都是对象
+ 给电话对象发一条响铃消息，它就会响
+ 拨号什么的略

Joe还写了个图形程序用来做可视化，但是由于当时跑在Sun工作站上的smalltalk很慢，他经常趁着GC的时候喝杯咖啡并休息一下。1986年之后公司买了台Tektronix Smalltalk机用来提高他们的工作效率，但是等待交付的时间太长了，在此期间，Joe一直在改进他的图形表示。

直到有一天他给Roger Skagervall展示了一下他的工作成果，Roger回答说这其实是个prolog程序，然后非常快地用prolog做了一个原型 - 这就是Erlang的开始了。

### 构想

于是当smalltalk机终于交付，Joe已经对smalltalk没什么兴趣了。他学会了使用prolog编写元解释器，并且搞清楚了怎么在prolog里同时跑多个任务。

### ACS/Dunder

### 疯狂的活动

### 带缓冲区的消息接收

### 错误处理

### 链接(Links)

### 缓冲区(Buffers)

### 编译目标是Strand!

### 

## 成长期-1989到1997

### ACS/Dunder项目的结果

### 走出爱立信

### 高效erlang VM实现 - JAM

### 语言设计上的改变

### receive为何被修改

### 岁月流逝

### BEAM诞生

### 分布式的erlang

### 传播过程

### AXE-N的消失

1995年12月完蛋

### OTP与所谓"行为"

### 更多语言设计上的变化

### 函数式编程范式的影响

### 二进制与位语法

### 数据库管理系统Mnesia

### 高性能erlang

### 向erlang加入类型推导的尝试

erlang的历史自一个prolog中的元解释器开始，所以很自然地顺承了prolog的动态类型.

第一组试着给erlang加类型系统的勇者是Phil Wadler和Simon Marlow

> One day Phil phoned me up and announced that a)Erlang needed a type system, b) he had written a small prototype of a type system and c) he had a one year’s sabbatical and was going to write a type system for Erlang and “were we interested?” Answer — “Yes.”

但是这活比看起来难干,最终他们只实现了一个erlang子集的类型检查，缺少进程类型和进程间消息的类型检查。虽然这个类型系统没上过生产，但是现在erlang社区会用它的类型标注语法写给人看的类型注释。

之后又失败了几个项目，直到Dialyzer的出现。它是HiPE项目的一个副产品，为了高效地编译erlang而生(有类型可以生成特化的函数目标代码)。Dialyzer不会尝试推导出所有类型，但可以保证能推导出来的都是正确的，以及出现在用户面前的类型错误没有误报。

## 湍流中的青春期-1998到2001

### 成功！

### 来自爱立信内部的拒绝，以及随后的开源化

### 开源的erlang

### Erlang开发组出走，Bluetail成立

### IT泡沫潮及其破灭

2000年Bluetail公司被Alteon Web Systems公司收购，6个月后Alteon又被Nortel Networks收购。不幸的是快乐的时间总是短暂的，6个月后原来erlang组的人就在裁员大潮中离开了一半。

## 成年-2002至2005

### 面向并发编程及其未来

### 成年期的erlang生存境况

### 从错误中学到的一课

### 终末

