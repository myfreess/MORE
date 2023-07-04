# 文本观察/C语言有害健康

在进入大学的第一个学期我初步学习了C语言，那时候我还不知道围绕着C/C++语言社区的一种神秘现象：语言律师，所以我可以编写C语言程序。现在我失去了这一能力。

大概我是所谓的指责型人格，现在我只想指责C语言。虽然如此，有很多人为保护C程序员的现代生活(?)做出了艰苦卓绝的努力。我只做个搬运工，把一些经过选择的既成事实放在这里。现在网上很流行所谓的3分钟看电影，我是个连基础剪辑都不会的愚钝网上流民(2023.3.24, 学了一点kdenlive)，但是我也想跟跟风，把我看到的关于C语言的一些博客文章，论文收集起来，做一个速读合订本。

此处送上我在知乎评论区看到的一段对话

> A: 解释对了，然后初学者觉得麻烦，不想学了？语言从来不是本质，谭书看看入个门就挺好。
> B: 解释对了就让人觉得麻烦的语言，本来就不适合用来入门。

## Fun with NULL Pointer 

**原文：http://lwn.net/Articles/342330/**

**作者：Jonathan CorbetJuly**

这是一个真实发生在2.6.30版本Linux内核中的故事，由于gcc编译器的激进优化，攻击者可以通过内核代码中一个对NULL指针的访问载入自定义的攻击载荷。

具体一点讲，这事发生在内核的TUN/TAP驱动代码。它提供虚拟网络服务，通常使用它的方法是打开`/dev/net/tun`, 然后用一个ioctl()调用设置网络终端。 Herbert Xu发现由于缺少数据包计数机制，有时某个应用会产生大量的内核内存占用，这直接导向整个系统的性能下滑。他的处理方案是[这个](http://git.kernel.org/?p=linux/kernel/git/torvalds/linux-2.6.git;a=commitdiff;h=33dccbb050bbe35b88ca8cf1228dcf3e4d4b3554)补丁,一个问题解决，另一个问题被引入。

TUN是支持`poll()`的，在2.6.30它的开头大概看起来像这样：

```c
static unsigned int tun_chr_poll(struct file *file, poll_table * wait)
    {
	struct tun_file *tfile = file->private_data;
	struct tun_struct *tun = __tun_get(tfile);
	struct sock *sk = tun->sk; //这一行是 Herbert的补丁
	unsigned int mask = 0;

	if (!tun)
	    return POLLERR;
```

补丁的问题是在检查tun是否非空之前便解引用了它。GCC对这段代码大胆地做出了以下推断

+ tun在被检查前就已经解引用过了
+ 用户不会编写解引用空指针的代码
+ 那tun一定是非空的，则下面的检查是不必要的。

> 此处GCC的推断依据便是所谓的未定义行为(undefined behavior，简称UB),更多批评详见后文

于是GCC把
```c
	if (!tun)
	    return POLLERR;
```
优化掉了。

而由于某些巧合，那时的安全机制允许用户空间的程序将地址空间的第零页映射到某块可以访问的内存上，为后续利用打开了一扇窗口。

## 工业级项目的构建脚本滥用UB

某位推特用户于2022年6月10日发推抱怨FFmpeg的configure脚本使用一段样例代码中的负数下标(显然是未定义行为)来测试目标机器是否为32位，随后另一位推特用户提醒他FFmpeg的configure脚本是手写的，而非通过gnu autoconf生成。但悲剧的是，autoconf为了兼容不支持C11的编译器同样不会在自动生成的脚本中使用符合标准的static_assert，就连gcc的构建脚本也是维护人员自己改过的。

```
do you ever think about how the industry-standard way of configuring C projects is to generate a script that tests the host C compiler against hundreds of dummy programs, reporting whether or not they throw errors

for example: https://github.com/FFmpeg/FFmpeg/blob/605b4016b341a0e8035c16d1890e7ddbf891badb/configure#L5394… to test if the host is 32 or 64 bit on x86, `./configure` just declares an array ``` int test[2 * (sizeof(void *) > 4) - 1]; ```

if a pointer is 32 bits, the expr reduces to `int test[-1];`, which will throw an error on GCC. from what I can find in the C spec, negative sized arrays are undefined behaviour, not explicit errors. depending on the host compiler this test could only warn or report incorrectly

I don’t know if newer versions of autoconf use `static_assert` in the outputted configure scripts but in general, there is an unbelievable amount of jank throughout the entire C ecosystem that terrifies me.

I have a hard time believing anyone can make a case for safe C when the build process itself relies on undefined behaviour. “uhh durrrr programmer discipline” is a weak boomer argument that instantly tells me someone is way too stubborn to move on with the times.

actually, I don't know if FFmpeg's configure script was written by hand or not. here's another example from GCC (autoconf generated) https://github.com/gcc-mirror/gcc/blob/a05aac0a130cd4e09530ebeb6beae4e5495ad6bc/configure#L4867… seems like `static_assert` is used on most hosts, save for weird non-ANSI-conforming compilers from compaq and IBM.

-- 以下为回复 --

The FFmpeg one is definitely handwritten, not autoconf. That GCC one uses static_assert in handwritten checks to detect if we have a C++11 compiler, but autoconf itself doesn't use static_assert in its own auto-generated checks. 1/2

Using static_assert would require assuming you already have a C11 compiler, and if you can assume that, you wouldn't bother using autoconf! If you know you have C11, no need to test for basic things like "is this C compiler from some time after 1982 or am I in hell?" 2/2
```

我一开始也以为这是完全的滥用UB,但是后来经群友提醒发现，至少在C99里面，这样做是有其合理性的，详见5.1.1.3，在preprocessing translation unit / translation unit包含对语法规则或约束的违反时，即使行为是未定义/实现定义的, 也应该有至少一条诊断信息(diagnostic message)。

但是我还是觉得这句话很在理：

> **If you know you have C11, no need to test for basic things like "is this C compiler from some time after 1982 or am I in hell?"**


## intmax_t 地狱

**原文：A Special Kind of Hell - intmax_t in C and C++**

`intmax_t`类型被设计用于提高应用的可移植性，但由于共享库的大量使用，这一愿景变成了彻头彻尾的空头支票。正如原文所说：*We cannot upgrade seamlessly.* 

> 也没有标题说得那么可怕，glic和musl libc之类的库通过一种叫做symbol versioning的老旧技术避免了类似的ABI Breaking问题，详情可直接看同作者的另一篇博客：**To Save C, We Must Save ABI**

作者用幽默风趣的语言表达了他对C诚挚的爱

```
“But C Is ABI-Stable?!”
Not necessarily. C is a simple language, and it both sells itself on and prides itself as such. So much so, that it’s even part of the [language’s rolling charter](https://www.open-std.org/jtc1/sc22/wg14/www/docs/n2021.htm).
There’s barely any name mangling because there’s no overloading. If you want “virtual functions” you need to hand-craft your virtual table structure and initialize it yourself. There’s barely any lookup or entity negotiation: what you write – [however scary or cursed](https://twitter.com/thingskatedid/status/1328918322507706368) – is what you get, in a general sense. (No, it’s not “portable assembly”. Compilers tear C code apart and make it far more efficient than the code people stuff into it. It’s not even a direct model of the machine anymore: just an abstract one.)
```

虽然如此，还是有一些聊胜于无的补救措施的。

可以用类似

```c
#define imaxabs __glibc228_imaxabs
```

这样的宏进行封装，**It is a bit like artisanal, hand-
crafted, free-range, and organic ABI versioning (or, as I have affectionately come to call it: personal masochism to make up for language failures).**

大多数时候它可以工作，少数时候不行。详情请阅读C标准的§7.1.4, 这一小节的名字是"ABI Breaks Guaranteed".

其实不是，这一小节真正的标题是"Use of library functions", 原文整段转载于此：

> Any function declared in a header may be additionally implemented as a function-like macro defined in the header, so if a library function is declared explicitly when its header is included, one of the techniques shown below can be used to ensure the declaration is not affected by such a macro. **Any macro definition of a function can be suppressed locally by enclosing the name of the function in parentheses**, because the name is then not followed by the left parenthesis that indicates expansion of a macro function name. For the same syntactic reason, it is permitted to take the address of a library function even if it is also defined as a macro. **The use of`#undef` to remove any macro definition will also ensure that an actual function is referred to.**

简而言之，为了避免函数被同名的类函数宏遮盖，有俩种方式绕过宏，一种是给函数名加括号

```c
(max)(value0, value1)
```

另一种方法是使用`#undef`.除此之外，一些实现也提供了一些手动指定函数对应符号的方法。

作者还很有幽默感地给出了一个C++的解决方案

```c++
namespace std {
  extern "C" __int128_t __i128abs (__int128_t v2) noexcept;

  using intmax_t = __int128_t;

  struct __imaxabs {

  private:
    using __imaxabs_ptr = intmax_t(*)(intmax_t) noexcept;

  public:
    constexpr __imaxabs_ptr operator& () const noexcept {
    return &__i128abs;
    }
    constexpr operator __imaxabs_ptr () const noexcept {
    return &__i128abs;
    }
  };

    inline constexpr const __imaxabs imaxabs = __imaxabs{};
}
```

> 我认为这就是一种语言自信

开玩笑的，原作者抱怨了C++的保守和对C标准库内容的大量保留，说C++本有机会成为更好的C，*But we explicitly choose for it not to be.*

不过这位仁兄着实是有能人士，他在另一篇博客**To Save C, We Must Save ABI**中不光指出了C的问题症结，还给出了他亲手操刀实现的一套手术方案。

> 加一条从群友处得知的私货：C99/C11的预处理算术依赖于intmax_t/unitmax_t语义, 但是在对字符常量做算术时又会有一点微妙的差异

## 救C先救ABI

**原文：To Save C, We Must Save ABI**


## C压根不算程序设计语言

原文：C Isn't A Programming Language Anymore

C语言被误解了，它实际上是编程世界中的*lingua franca*(通用语)，**it’s a protocol that every general-purpose programming language needs to speak.**

## 为什么C语言从不阻止你犯错

**原文：https://thephd.dev/your-c-compiler-and-standard-library-will-not-help-you**

## C语言不适用于操作系统开发

兼论：*未定义行为 VS 维特根斯坦*

原文：

+ **How ISO C became unusable for operating systems development**
+ **Depressing and faintly terrifying days for the C standard - C STANDARD UNDEFINED BEHAVIOR VERSUS WITTGENSTEIN**

同一名作者(Victor Yodaiken)的论文+博客 - 几个论据是重复的，写作意图不太一样。

后者的写作时间较早且作为博客文章没什么主旨，作者只是在结尾给了个不大客气的评价：

> Either C will fade away as Lattner and many others hope, the standard will change, or there will be a successful fork to produce a more precise and flexible standard.
> 
> (要么ISO C语言革自己的命，要么大家搞个C方言然后逐步迁移过去)

论文则思考地更加深入：

> Limitations of ISO C for OS development have been noted in academic literature:
> 
> "Systems or library C codes often cannot be written in standard-conformant C" [20].
> 
> and by practitioners e.g. [38] The primary cause is a design approach in the ISO standard that has given priority to certain kinds of optimization over both correctness and the "high-level assembler" [9] intentions of C, even while the latter remain enshrined in the rationale.
>
> (完全遵守ISO C标准没法写系统/库代码，主要原因是制定标准时过分看重优化而不太在意语义正确性和C作为高级汇编的设计意图。)

顺带一提这作者似乎偏好K&R C

> ISO delegates to the compiler a great deal of the control **that K&R C divides between the programmer, the environment, and the target architecture but not the compiler**
>
> (ISO C把许多K&R C里面由程序员、环境、目标架构控制的东西托付给了编译器)

在抛出论点之后VY很快给出了一个实例

> For an example of an implementation of malloc in K&R(page 187) the text explains there is a question about whether "pointers to different blocks ... can be meaningfully compared", something not guaranteed by the standard. The conclusion is "this version of malloc is portable only among machines for which general pointer comparison is meaningful." – delegating the semantics to the processor architecture.
>
> (K&R C的malloc语义在符合要求的架构上一致可移植)

博客和论文在正式开始大鸣大放之前都选择引用一段名言，可能这就是叠甲吧。论文选择的是C语言之父丹尼斯里奇对ISO C前身ANSI C草案的批评：

> Dennis Ritchie. 1988. noalias comments to X3J11. (March 1988). https://groups.google.com/g/comp.lang.c/c/K0Cz2s9il3E/m/YDyo_xaRG5kJ
>
> "The fundamental problem is that it is not possible to write real programs using the X3J11 definition of C. The committee has created an unreal language that no one can or will actually use."
>
> (委员会搞出了一个没什么实用价值的语言)

博客则找来Clang编译器主开发者之一Chris Lattner在博客中对未定义行为的意见：

> "UB is an inseperable part of C programming, […] this is a depressing and faintly terrifying thing. The tooling built around the C family of languages helps make the situation less bad, but it is still pretty bad. The only solution is to move to new programming languages that dont inherit the problem of C. Im a fan of Swift, but there are others.[4]"
>
> (UB很糟糕，UB没办法从C语言里面分离出去，就算有工具的帮助UB还是非常糟糕，还是赶快找个和C没什么血缘关系的语言保平安吧！)
> 
> "[…] many seemingly reasonable things in C actually have undefined behavior, and this is a common source of bugs in programs. Beyond that, any undefined behavior in C gives license to the implementation (the compiler and runtime) to produce code that formats your hard drive, does completely unexpected things, or worse"


定性批判结束后，紧接着的下一步是向ISO C关于别名的语言设计开火，这作者还是选择先摘录一遍Linus和丹尼斯的批评：

> Ritchie’s main objection was to a type attribute intended to limit aliasing (two or more active pointers/references addressing the same storage).
> 
> "the committee is planting timebombs that are sure to explode in people’s faces. Assigning an ordinary pointer to a pointer to a ‘noalias’ object is a license for the compiler to undertake aggressive optimizations that are completely legal by the committee’s rules, but make hash of apparently safe programs."

里奇这还算客气的, Linus作为开源世界知名大喷子，批评更是饱含个人感情:

> The idiotic C alias rules aren’t even worth discussing. They were a mistake. The kernel doesn’t use some “C dialect pretty far from standard C”. Yeah, let’s just say that the original C designers were better at their job than a gaggle of standards people who were making bad crap up to make some Fortran-style programs go faster. They don’t speed up normal code either,they just introduce undefined behavior in a lot of code. And deleting NULL pointer checks because somebody made a mistake, and then turning that small mistake into a real and exploitable security hole? Not so smart either. [8]
>
> (想看更多更强的攻击性可以打开这个链接：https://lkml.org/lkml/2018/6/5/769)

发完牢骚之后，还是要把批判落到实处的，VY举了几个关于别名规则的糟糕例子：





## C语言的三权分立

**原文：Into the Depths of C: Elaborating the De Facto Standards**

顺便奉上两篇我个人觉得和它有点关联的文章：

+ **Pointers Are Complicated, or: What's in a Byte?**
+ **A Few Billon Lines of Code Later - Using Static Analysis to Find Bugs in the Real World**

> 一切的一切都导向这样一个结果：一致且连贯的C语言从来就没有存在过，将来也不会存在。

2013年，这篇文章的创作团队向一些C语言方面的专业人士(委员会成员，C分析工具开发者，C形式语义专家，编译器作者，系统程序员)提出了42个关于C语言标准的问题。第二次调查于2015年展开(通过技术邮件组和博客)，得到了323条回复。他们从调查结果中得以一窥系统程序员所暗自期许的那个"C语言事实标准"(de facto Standard), 不幸地，和ISO标准不是同一个。



## GCC、strict aliasing与恐怖故事

**原问题：https://stackoverflow.com/questions/2958633/gcc-strict-aliasing-and-horror-stories**

## C标准就是坨狗屎

**原邮件：https://lkml.org/lkml/2018/6/5/769**