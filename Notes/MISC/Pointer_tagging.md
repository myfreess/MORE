> 原文：Pointer tagging for x86 systems
> 作者：Jonathan Corbet

64位系统已经成为现代PC的标配，然而，其系统中的指针宽度却通常达不到64bit这么宽。

指针高位的未使用区可以被用于存放各种元信息。内存分配器使用它追踪不同的内存池(垃圾收集器可如法炮制)，各类应用(如数据库管理系统)也都有它们自己的用法。但使用这种技巧时需额外小心，在解引用此指针/将它传给逻辑上不涉及元信息的代码时需要小心地用掩码遮住元信息。这容易出错误，而且会拖慢应用速度。

为了让开发者的日子好过些