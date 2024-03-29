# 文本观察/Ur/Web: A Simple Model for Programming the Web

World Wide Web从一个文档递送平台逐步转变为一个分布式编程架构(an architecture for distributed programming), 巨大且无计划的野蛮生长最终呈现为一组任何web应用都必须管理的互相连接的语言与协议。Ur/Web是一个被设计用于Web编程的静态类型函数式语言，它提供了一个编写现代Web应用的简化模型.

Ur/Web语言设计两大要素：封装与简单的并发模型

封装基于ML风格的模块系统，让模块实现特定的signature，外部使用时没法拿到具体的类型，只能用signature里提供的几个函数

Ur/Web由Ur语言扩展而来，而Ur是一个有依赖类型的静态类型函数式语言。

Ur/Web的编程模型基本上是把Web看做一个应用平台:

+ 一个应用应该是个用单一语言编写(Ur/Web)编写并且运行在一个服务器和诸多客户端的程序，不同部分应该被自动编译为合适的目标语言(比如JavaScript)
+ 所有在应用的不同部分之间传递的对象都应该是带类型的，一块代码完全可以是first-class的对象，以AST方式传递(避免各种嗯拼字符串导致的悲剧)
+ 唯一的持久化状态就是SQL数据库
+ 服务端会提供一组可以远程调用的函数(同样是类型化的接口)，客户端代码在远程调用它们时必须使用类型正确的参数，最后返回给客户端生成HTML文档
+ HTML里面可以嵌入Ur/Web代码，以协程模型运行，前端GUI采用FRP.
+ 客户端可以发起阻塞的远程函数调用
+ 服务端代码可以生成带类型的消息传递频道(typed message-passing channels),它可以通过远程函数调用返回给客户端，这也是Ur/Web提供的一种异步通信方式，不过好像只能服务端发客户端收。