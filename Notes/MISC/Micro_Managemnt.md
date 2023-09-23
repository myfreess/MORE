Mike Pall, author of LuaJIT, decided to write LuaJIT 2.x’s interpreter in assembly rather than C, and he cites this decision as a major factor that explains why LuaJIT’s interpreter is so fast. He later went into more detail about why C compilers struggle with interpreter main loops. His two most central points are:

    The larger a function is, and the more complex and connected its control flow, the harder it is for the compiler’s register allocator to keep the most important data in registers.

    When fast paths and slow paths are intermixed in the same function, the presence of the slow paths compromises the code quality of the fast paths.

Our design does away with a single big parse function and instead gives each operation its own small function. Each function tail calls the next operation in sequence