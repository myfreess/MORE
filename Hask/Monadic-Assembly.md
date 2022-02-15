> Monad是一种Context Sensitive的CodeGen -- 笔者言

这话该是何时说的？应该有几个月了(当前时间: 2022.2.7)，当时是在讨论IO Monad为什么是纯的，笔者好说怪话，于是抛出了一篇暴论博客**C is purely functional language**,进而迫真论证图灵机也是纯函数式，顺便还说了上面那番话。

之后有群友发给我一篇博客:

https://www.stephendiehl.com/posts/monads_machine_code.html

好吧，其实只是群友顺手分享，不是他发给我一个人看的，我只是习惯性地在给自己脸上贴金。这唤醒了我冻结的记忆，赶忙翻出收藏中的俩篇博客

http://www.wall.org/~lewis/2013/10/03/inverting.html

http://wall.org/~lewis/2013/10/15/asm-monad.html

因为笔者是野路子出身......算了不编了，由于复杂的历史遗留笔者在操作系统，编译原理和硬件这块没学好......隐含的意思就是没提到的也不咋样了，所以畏难情绪强，因为这俩篇博客涉及汇编，所以收藏了一直没看。

顺便，在动手写下这份笔记的时候，还看到有人在F#上实现了一个类似的EDSL，毕竟F#也是有计算表达式的嘛。

大概描述一下思路。

这是一行x86汇编(默认Intel语法, 目标操作数在前面)

```assembly
xor eax, esi
```

但是既然我们打算在haskell里把它实现成一个EDSL，那它就应该是个普普通通的值。

大概有人会想到，用ADT描述一个x86指令的子集

```haskell
data Val
  = I Int64      -- Integer
  | R Reg        -- Register
  | A Word32     -- Addr
  deriving (Eq, Show)

data Instr
  = Ret
  | Mov Val Val
  | Add Val Val
  | Sub Val Val
  | Mul Val
  | IMul Val Val
  | Xor Val Val
  | Inc Val
  | Dec Val
  | Push Val
  | Pop Val
  | Call Val
  | Loop Val
  | Nop
  | Syscall
  deriving (Eq, Show)
```

那么下一步呢？大概是直球编译吧。一段机器码刨除其内蕴的逻辑和抽象，那不过就是一段字节流，用`[Word8]`就能表示。

```haskell
assembler :: [Instr] -> [Word8]
-- Data.Word模块
-- Word8用于表示单个字节，不过仍然是个包装过的ADT
```

这有点像什么？haskell的早期流式IO！但是比起真正的汇编这其实还有所不如了 -- 没label, 很难复用。所以我们需要monad。

准确来说，此处需要的是一个State Monad的变体

```haskell
type X86 a = StateT JITMem (Except String) a
```

状态类型JITMem是个不算简单的Record，它包含这些信息：

+ “源码”(在这里是直接记录上面的指令集类型了，毕竟EDSL也没有parse步骤)
+ 对应的机器码
+ 指令计数，不过这个虽然在代码里有，Stephen Diehl却没使用。
+ 指令在内存中的起始地址，这个是逻辑上不应该改变的常量。
+ 当前指令的内存偏移量，用于扮演label。
  
> 比较生草的是Stephen的初始化函数里面已经把偏移量的初值设成起始地址了，但是后面的代码里他还总是`gets _memptr`，然后拿出来又不用......

```haskell
data JITMem = JITMem
 { _instrs :: [Instr]
 , _mach   :: [Word8]
 , _icount :: Word32
 , _memptr :: Word32
 , _memoff :: Word32
 } deriving (Eq, Show)
```

有些人已经可以想象到做法了，但是我还是去抄了下作业才明白：这是个简单的jit，通过State Monad编写汇编生成器，给定一个初始地址之后生成机器码，然后放进内存(利用GHC的FFI)执行。

框架已经明晰，但是痛苦的体力活和一些细节逃不开。

同时我也吐槽一下，第一次学x86汇编，感觉真的非常繁杂啊，怪不得大伙总是提RISC—V。




