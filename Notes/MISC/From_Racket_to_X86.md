> 基本上是Essential of compilation一书前几章的读书笔记

# 2 Integers and Variables

第二章主要内容：将一个包含变量和整数的racket子集(L_var)编译到x86-64

2.1节介绍L_var语言，2.2节介绍x86汇编(仅限编译所需)，2.3节讨论编译所需的步骤，此章余下部分对每一步骤实现给出提示。

## 2.1 The LVar Language

用let创建变量，为保持简单每个let只创建一个变量

### 2.1.1 Extensible Interpreters via Method Overriding

EoC一书中的解释器采用面向对象范式编写(尽管使用的语言是racket)，事出有因，在整本书里语言子集的体量是逐步增加的，每个语言的解释器都会有一部分(越到后面这部分占比越大)需要复用之前的解释器，但如果只使用racket平常那种偏函数式的风格会有一点困难 - 让一个只能做算术的解释器明白包含变量的算术表达式怎么处理有点强程序所难了。

OOP中的方法重写可以实现*open recursion*, 假设有一个`interp-Lint-class`类,它提供了`interp_exp`方法

```racket
(define interp-Lint-class
  (class object%
    (define/public ((interp_exp env) e)
       ......)))
```

在其扩展类`interp-Lvar-class`中重写此方法，则原本方法中的递归调用会自动重定向到扩展类的方法。

赋值，很奇妙吧!

> 想了想，不用class也行，需要一个装在ref里面的函数，差不多

### 2.1.2 Definitional Interpreter for L_var

感觉没啥值得记的

## 2.2 The x86Int Assembly Language

讲了点压栈之类的东西

## 2.3 Planning the Trip to x86

对比L_var和X86之间的语言特性区别，以及编译器pass

+ **uniquify** : 解决变量遮盖的问题，给每个变量rename一个全局唯一的名字
+ **remove_complex_operands** : 通过增加let使得每个primitive以及函数调用的参数不是整数便是变量
+ **explicate_control** : 明确执行顺序，将程序的抽象语法树转换为图，图节点为带标签的一连串语句，边为goto语句
+ **select_instructions** : 将每个L_var里的操作转换为等价的X86命令序列
+ **assign_homes** : 将变量替换为寄存器/栈位置

下一个问题是以怎样的顺序应用这些pass？这个问题可以很有挑战性，因为我们没法先验地得知哪种顺序更好(实现更简单，或者能产出更高效的代码，等等)。

### 2.3.1 The C_var Intermediate Language

**explicate_control**生成的IR很像C语言，这种风格的IR也常被称为*three-address code*，因为其典型语句形式`x = (op y z);`涉及xyz三个地址。

## 2.4 Uniquify Variables

书里提供了一个大致的代码骨架，并且给出提示：应该弄一个旧变量名与新变量名的关联列表，以及racket的gensym函数很适合用于生成新变量

## 2.5 Remove Complex Operands

突然开始monad(误)

其实是说分离纯表达式(atomic)和有副作用表达式的语言是*monadic normal form*。

**remove_complex_operands**必须维持的不变量是副作用顺序不能变

实现建议是弄俩互递归函数，以及使用多值返回。

## 2.6 Explicate Control

## 2.7 Select Instructions

由于read涉及sys call了，得链接一个C写的runtime

## 2.8 Assign Homes

由于寄存器分配在后面现在统统压栈

## 2.9 Patch Instructions

保证一条指令里面最多一个寻址

## 2.10 Generate Prelude and Conclusion

加上合适的调用约定

## 2.11 Challenge: Partial Evaluator for LVar

# 3 Register Allocation

CPU需要10到上百个时钟周期访问栈上的一处内存，而访问寄存器永远只使用单个时钟周期。显然，把所有变量都搞到栈上非常浪费。

有些程序中变量数量多于寄存器，则需要让一些变量分配到同一个寄存器上，问题在于如何找出那些不会互相干涉(interfere)的变量。

如果最后还是有些变量没法分配到寄存器上，那也只能压栈了，一般用*spill*来称呼这种行为。

为了简化问题，书中的寄存器分配假设某个变量不是分配到寄存器便是分配到栈上，更复杂的方法会在程序的不同区域为同一变量采用不同分配方案。例如，一个变量在某小块区域被频繁使用后又在相隔很多指令的一处地方被使用了，则先入寄存器然后spill。

## 3.1 Registers and Calling Conventions

由于需要调用read_int函数(以及生成了一个由操作系统调用的main函数)，不得不考虑调用约定(此处书中使用System V).

下文的子例程(subroutine)就是C的函数，应该是这样吧。

**调用者的规则(prologue)：**

+ 在子例程调用前，保存r10，r11，以及所有用于传参的寄存器的内容(入栈)。如果子例程调用之后恢复上下文，就放到栈上。这些寄存器叫做*caller-saved registers*

+ 子例程的前六个参数按顺序放进`rdi rsi rdx rcx r8 r9`，剩下的逆序放到栈上(最后一个参数第一个进栈)。在栈向下的增长完成之后，第七个参数会被放在最低位的地址(逆序在历史上曾用于支持可变参数，大概因为x86和C都是热衷于兼容的也便留下来了。)
  
+ 用call指令调用子例程，它会把返回的地址放在栈顶，然后切换到子例程。

+ 在子例程返回后，调用者必须把放在栈上的额外参数全部弹出，将状态复位到调用前。

+ 调用者可以期待在RAX寄存器中得到子例程的返回值。

+ 把子例程调用前保存的寄存器内容从栈中弹出并复位。调用者可以假设子例程没修改过其他寄存器。

**被调用者守则(epilogue)：**

+ 内部变量要么分配个寄存器，要么在栈上弄块空间。再重复一遍，栈是向下增长的，所以在栈顶放东西占用了空间之后要把栈指针递减.至于具体要减多少，得看用了多少内部变量。举例，如果有一个float和一个long(共占12字节)，那就应该

  ```assembly
  sub rsp, 12
  ```

+ RBX，RBP，R12 - R15是被调用者有义务去保存内容的寄存器，当然了，还是放到栈顶。RSP按照调用约定也应该被保护，但是并不需要将其内容推入栈。这些寄存器叫做*callee-saved registers*

+ 返回值应该放在RAX寄存器中。

+ 之前保存的寄存器内容应当从栈中弹出并复位。

+ 将RSP的内容(栈指针)恢复到原值。

+ 通过ret指令返回。

不过呢，如果去看一些C编译器的中间产物，也许会在子例程的前部看到一些额外的指令

```assembly
; 被调用者开始
push rbp
mov rbp, rsp
......
; 在ret前
pop rbp
```

这是从32位的调用约定套来的一个历史遗留，可以通过`-fomit-frame-pointer`这个flag去除这些冗余代码。

那么调用约定如何影响寄存器分配？

如果一个变量的生命周期中包含函数调用(这种变量被称为*call-live variable*)，它不应该被分配到*caller-saved registers*上，这样效率不高。

由于EOC这一章使用图着色算法搞寄存器分配，只要使用*interference graph*即可。

另一方面，对于并非call-live的变量，分配到*caller-saved registers*上可以节省空间。

书里建议把变量对应到自然数上，每次选择最小且无interference的数

## 3.2 Liveness Analysis



