为阅读博客文章Hexing the technical interview所做的前置准备

https://aphyr.com/posts/341-hexing-the-technical-interview

主要内容包括

+ Class Loader

+ Class文件结构

+ 链表环路检测

最开始看到Aphyr的文章是在知乎上看到有人发了Typing The Technical interview的链接，查完资料通览全文之后就一个感觉：劲！

说来不好意思，因为文章里的文化背景不太好懂把代码拿出来胡乱脑补了个背景故事，后来发现有了翻译，非常开心。

https://zhuanlan.zhihu.com/p/84634204

2022年春节时我学习了Clojure(初步)，然后----

然后发现这篇文章的重点不是Clojure语言的技巧，而是JVM字节码

下面列出原文及我查找的对应资料

1.
> “What are these?”
> 
> “Magic numbers.” You are, after all, a witch. “Every class begins with a babe, in a cafe.”

如果把一个java字节码文件的内容看作一个巨大的结构体，那么位于开头的是一个大小四字节的魔数：`0xCAFEBABE`

穿越时空的幽默啊。

2.
> "We’re using version 49 because it doesn’t require stack maps, which keeps things simple. 
 
0xCAFEBABE后面的数据是字节码版本号，先小版本后大版本，此处49为大版本。

采用版本49的理由是版本50.0之后的字节码文件按照JVM标准要求，运行之前要为字节码做个类型检查。一些简单的类型信息被记录在StackMapTable这个Atrribute里，令人惊讶的是这些信息中居然还包括变量是不是Null乃至有没有初始化。

类型检查规则(的形式化版本)是用Prolog语言描述的，虽然很早就听说过，但是百闻不如一见嘛。

3.
>   0x00 0x17                 ; 22 constants
> 
>  “I’m sorry,” Tim blinks. “But isn’t 0x17 decimal 23, not 22?”
>
> “Og én,” you recite, sing-song, “Til javanissen!”
>
> “Beg pardon?”
>
> “The javanisse. Surely you have heard of him! He is a small, magical man–something like a gnome–who inhabits every JVM. If you do not set out an extra constant for him, he can cause segfaults.

当初学习Java的String的时候被告知字面量会在常量池里面待着，现在才知道其实字节码开头就有个常量池(这个不光是字符串常量，还有类名接口名字段名之类的)。在常量池前面是什么呢？答案是常量池计数，按照规定常量池计数还应该比真实条目数量要多个一。

考虑到原文主角是个魔女，在Tim惶惑不已怀疑小学数学学歪了的时候拿什么JVM中的段错误小精灵忽悠他也算是合理的人物塑造了，迫真程序员爽文

4.
> “Method names start with lowercase letters,” Tim asserts. His voice rises like a question.
> 
> “Only by convention. Almost any string will do, and we already have this one in the constant pool.”

不能拿javac的剑打JVM的人民公仆啊，主角在此处将测定链表循环的方法命名为"Code", 大写字母开头，但是这个常量实际上不是专门给方法用的，重用常量是因为这里在炫技要压缩字节码体积，常量池都手写了。

话又说回来，那"Code"这个常量原来是给谁用的？其实是标记Code这个Attribute的。

5.
> (.getBytes "(Ljava/lang/Iterable;)Z") ; Our arg signature

这玩意是所谓的"Descripter",用来描述一个字段/方法的类型的(当然了也是放在常量池里)。这里描述的是Code方法的类型 -- 不过跟平时习惯不太一样，返回值类型在括号后面。

L + 类名/接口名代指一个Object，Z则是boolean，从上面得知方法的名字是Code

```java
boolean Code(Iterable foo) { ... }
```

那么大概就是这样。

多个参数的方法签名写出来更加惨不忍睹，比如`Object m(int i, double d, Thread t) { ... }`, 写出来大概是

```
(IDLjava/lang/Thread;)Ljava/lang/Object;
```

虽然很丑，但是本来也不是给人类阅读的，JVM到处都在跑，若是给它增添parse负担岂不是加剧耗电和温室效应。

6.
> “Did you mean astore_2?” Tim asks, trying to be helpful. “Variable 0 holds our first argument, right?”
>
>“It did.” You agree. “But we won’t be needing it again.”
>
>“But… those aren’t even the same type. That’s… that’s illegal.”
>
>“If it were meant to be illegal,” you remind him sagely, “Sun Microsystems would have made it unrepresentable.”

这里主角为了节省栈空间直接把一个本地变量放到原先参数的位置上了，这大概也是选择字节码版本49放弃类型检查的原因之一，这个本地变量是一个Iterator而非一个Iterable的集合，类型上不一样。

为了确保没在纯胡扯翻阅了一下JVM规范的4.7.4，类型信息那里确实有记录具体是哪个类来着。

7.
> One will be the fast iterator. Her name is Jorunn, and her legs are strong from years of skiing. She flies forward with powerful strokes.
>
> Bjørn, in register 0, is fat and lazy. He ambles along like his namesake.
>
> Jorunn, not to be outdone, takes another stride. Her footing is sure.

这个就真的跟算法有关了，主角用于检测可变链表中是否具有回环的算法是[Floyd's cycle finding Algorithm](http://en.wikipedia.org/wiki/Cycle_detection#Tortoise_and_hare), 此算法的主要想法是让俩个持有的引用遍历速度不同，一个快一个慢，然后假如它们相遇了就说明有环，详情请见

https://stackoverflow.com/questions/2663115/how-to-detect-a-loop-in-a-linked-list



