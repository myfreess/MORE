> Monad是Context Sensitive的CodeGen -- 笔者言

这话该是何时说的？应该有几个月了(当前时间: 2022.2.7)，当时是在讨论IO Monad为什么是纯的，笔者好说怪话，于是抛出了一篇暴论博客**C is purely functional language**,进而迫真论证图灵机也是纯函数式，顺便还说了上面那番话。

之后有群友发给我一篇博客:

https://www.stephendiehl.com/posts/monads_machine_code.html

好吧，其实只是群友顺手分享，不是他发给我一个人看的，我只是习惯性地在给自己脸上贴金。这唤醒了我冻结的记忆，赶忙翻出收藏中的俩篇博客

http://www.wall.org/~lewis/2013/10/03/inverting.html

http://wall.org/~lewis/2013/10/15/asm-monad.html

因为笔者是野路子出身......算了不编了，由于复杂的历史遗留笔者在操作系统，编译原理和硬件这块没学好......隐含的意思就是没提到的也不咋样了，所以畏难情绪强，因为这俩篇博客涉及汇编，所以收藏了一直没看。

顺便，在动手写下这份笔记的时候，还看到有人在F#上实现了一个类似的EDSL，毕竟F#也是有计算表达式的嘛。


大概描述一下



