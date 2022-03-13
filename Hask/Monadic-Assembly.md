> Monad是一种Context Sensitive的CodeGen -- 笔者言

这话该是何时说的？应该有几个月了(当前时间: 2022.2.7)，当时是在讨论IO Monad为什么是纯的，笔者好说怪话，于是抛出了一篇暴论博客**C is purely functional language**,进而迫真论证图灵机也是纯函数式，顺便还说了上面那番话。

之后有群友发给我一篇博客:

https://www.stephendiehl.com/posts/monads_machine_code.html

好吧，其实只是群友顺手分享，不是他发给我一个人看的，我只是习惯性地在给自己脸上贴金。这唤醒了我冻结的记忆，赶忙翻出收藏中的俩篇博客

http://www.wall.org/~lewis/2013/10/03/inverting.html

http://wall.org/~lewis/2013/10/15/asm-monad.html

因为笔者是野路子出身......算了不编了，由于复杂的历史遗留笔者在操作系统，编译原理和硬件这块没学好......隐含的意思就是没提到的也不咋样了，所以畏难情绪强，因为这俩篇博客涉及汇编，所以收藏了一直没看。

顺便，在动手写下这份笔记的时候，还看到有人在F#上实现了一个类似的EDSL，毕竟F#也是有计算表达式的嘛。

概述一下思路:

这是一行x86汇编(默认Intel语法, 目标操作数在前面)

```assembly
; xor一般叫操作项，eax和esi一般叫操作数
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

## Types

x86-64指令集起初为intel的8086CPU家族设计，主要的数据类型包括整数和浮点数，虽然一一列举它们很乏味，但是没法躲开。

整型：4种变体

```
Byte           | 00 |

Word           | 00 | 00 |  

DoubleWord     | 00 | 00 | 00 | 00 |

QuadWord       | 00 | 00 | 00 | 00 | 00 | 00 | 00 | 00 |
```

大小区别一目了然，不过还有一事，即intel架构普遍采用小端字节序，这也是知名计算机模因了，仅举一例说明：

16进制数`0xC0FFEE`作为DWORD表示，内存布局如下

```
         |-- 高位--|-- 低位 --|
         +-------------------+
0xC0FFEE | EE | FF | C0 | 00 |
```

指针只是个地址空间中的地址 -- 用一个整型表示，其下的访问与存储全由操作系统内核包办，原文作者Stephen表示他用Linux，示例代码只考虑Linux x86-64环境。作者说把mmap和mprotect的FFI调用换一换也能拿到别的平台跑，但是在与C库交互时作者使用了在Unix平台上通行的System V调用约定，所以Windows版大概要费些功夫。

对于汇编操作数，大概不是数据便是寄存器或者地址 -- 别叫指针了，毕竟不是C. 那么简简单单写个ADT来表示

```haskell
data Val
  = I Int64      -- 64位整数，有符号
  | R Reg        -- 寄存器，Reg类型就是个枚举
  | A Word32     -- 地址
  deriving (Eq, Show)
```

下一步，了解寄存器。

## Register

1946年，亚瑟，赫尔曼和约翰冯诺依曼在普林斯顿聚首，为他们所谓的“电子记忆器官”起草了一个设计方案。他们的想法之一基于现实世界的限制：无限量的快速存储能力做不到，那就弄个分级存储器体系，每一级的存储空间胜过上一级，代价是读取速度变慢。常用的数据自然应该放在能快速拿到手的地方，对吧。

对于程序员而言，会在编程中接触到的最小存储器大概就是它了：寄存器。我第一次认真地尝试了解它是在那本著名的科普书**编码：隐匿在计算机软硬件背后的语言**上，但是认识层次始终停留于“它是个存储器”。

x86架构下有16个通用64位寄存器，放quadword完全够。其中声名在外的有：`rax, rbx, rcx, rdx, rbp, rsi, rdi, rsp`

```haskell
data Reg
  = RAX  -- 累积器
  | RCX  -- 计数器 (循环计数器)
  | RDX  -- 数据
  | RBX  -- 基础，通用
  | RSP  -- 当前栈指针
  | RBP  -- Previous Stack Frame Link
  | RSI  -- Source Index Pointer
  | RDI  -- Destination Index Pointer
  deriving (Eq, Show)
```

它们有编制的，不只是名字，还有数字对应。

```
RAX RBX RCX	RDX	RBP	RSI	RDI	RSP
 0	 1	 2	 3	 4	 5	 6	 7
```

这编号会用于一些特殊的寄存器操作指令(二进制表示中会用到)。

```haskell
index :: Reg -> Word8
index x = case x of
  RAX -> 0
  RCX -> 1
  RDX -> 2
  RBX -> 3
  RSP -> 4
  RBP -> 5
  RSI -> 6
  RDI -> 7

-- 偏函数注意
```

其实x86还允许把这些个64位寄存器当成更小的寄存器使用(名称上大概就是从rax到eax再到ax), 不过还是让我们不去管这些个繁文缛节吧。

