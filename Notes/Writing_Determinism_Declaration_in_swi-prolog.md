# swi-prolog系统中的确定性声明

swi-prolog吸收了一点Mercury语言的设计，你可以标记一个谓词的"确定性"

+ det - 确定性的转换(例如算术)，明确只成功一次。
+ semidet - 明确只成功/失败一次，如var/1这种类型判断谓词或者测试
+ failure - 永远失败
+ nondet - 不限制成功次数
+ multi - 会有多次选择，且至少能成功一次，和nndet一般用于数据生成器
+ undefined - 不真也不假的第三值


确定性往往和某个谓词被实例化的参数有关 - 所以还要配合参数上的*实例化模式*(instantiation patterns)一同使用

这个实例化模式约莫有这么几种：

+ `++` - 参数在调用时是一个非常饱满的term，不含任何逻辑变量
+ `+` - 参数在调用时被完全实例化为一个符合该类型的term，但是内部可能包含逻辑变量，如`[_]`, 它是一个只有一个元素的List，但是这个元素是一个无任何绑定的变量
+ `-` - 参数是一个所谓的"输出"参数，可以无绑定，也可以有，后面一种情况最终搜索出的结果会尝试与给定的结果进行合一，例如`findall(X, Goal, [T])`, det标记会假定它在调用时是一个自由变量。在该参数有绑定的情况下，det退化为semidet，multi退化为nondet
+ `--` - 参数在调用时绝对无绑定，在用于创建某种对象的谓词中很常用(比如创建一个IO流)，具体的例子可参考open/4
+ `?` - 参数在调用时被实例化为一个partial term，注意逻辑变量是任意类型的partial term。
+ `:` - 参数是一个元参数，一般用于高阶函数及元编程(传递一个functor)
+ `@` - 参数不会被进一步实例化，即谓词内部不会对参数进行合一。一般用于var/1这样的类型测试, 毕竟在prolog里面，逻辑变量也只是一种特定类型的数据。
+ `!` - 变量包含可被setarg/3之类谓词修改的可变结构



下面是一个示例

```prolog
%!      length(+List:list, -Length:int) is det.
%!      length(?List:list, -Length:int) is nondet.
%!      length(?List:list, +Length:int) is det.
%
%       True if List is a list of length Length.
%
%       @compat iso
```

稍等，你看出来这个例子哪里有问题了吗？

所谓的partial term，指的是某一部分结构被逻辑变量代替的term, 例如`[3,4 | X]`，其列表尾部为逻辑变量X。那么上面这个List的长度应该是：大于等于2(在没有循环的情况下)，因为X可以合一到任意一个List.

```
?- length([3,4 | X], 1).
false.
```

所以`length(?List:list, +Length:int)`的确定性应该是semidet。

