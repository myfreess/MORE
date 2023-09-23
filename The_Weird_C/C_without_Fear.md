# 不再害怕C语言

# 名人名言

C语言之父丹尼斯里奇对ISO C前身ANSI C草案的批评：

> Dennis Ritchie. 1988. noalias comments to X3J11. (March 1988). https://groups.google.com/g/comp.lang.c/c/K0Cz2s9il3E/m/YDyo_xaRG5kJ
>
> "The fundamental problem is that it is not possible to write real programs using the X3J11 definition of C. The committee has created an unreal language that no one can or will actually use."
>
> (委员会搞出了一个没什么实用价值的语言)
>
> "the committee is planting timebombs that are sure to explode in people’s faces. Assigning an ordinary pointer to a pointer to a ‘noalias’ object is a license for the compiler to undertake aggressive optimizations that are completely legal by the committee’s rules, but make hash of apparently safe programs."

Clang编译器主开发者之一Chris Lattner在博客中对未定义行为的意见：

> "UB is an inseperable part of C programming, […] this is a depressing and faintly terrifying thing. The tooling built around the C family of languages helps make the situation less bad, but it is still pretty bad. The only solution is to move to new programming languages that dont inherit the problem of C. Im a fan of Swift, but there are others."
>
> (UB很糟糕，UB没办法从C语言里面分离出去，就算有工具的帮助UB还是非常糟糕，还是赶快找个和C没什么血缘关系的语言保平安吧！)
> 
> "[…] many seemingly reasonable things in C actually have undefined behavior, and this is a common source of bugs in programs. Beyond that, any undefined behavior in C gives license to the implementation (the compiler and runtime) to produce code that formats your hard drive, does completely unexpected things, or worse"

Linus

> "The idiotic C alias rules aren’t even worth discussing. They were a mistake. The kernel doesn’t use some “C dialect pretty far from standard C”. Yeah, let’s just say that the original C designers were better at their job than a gaggle of standards people who were making bad crap up to make some Fortran-style programs go faster. They don’t speed up normal code either,they just introduce undefined behavior in a lot of code. And deleting NULL pointer checks because somebody made a mistake, and then turning that small mistake into a real and exploitable security hole? Not so smart either. "
>
> (想看更多更强的攻击性可以打开这个链接：https://lkml.org/lkml/2018/6/5/769)

# 所谓未定义行为

# ABI问题

# C对硬件的制约

# 一些改良措施