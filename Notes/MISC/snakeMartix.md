如何用程序输出这样的N阶方阵？

```
1 3 4
2 5 8
6 7 9
```

代码

```haskell
f i j = let {z = i + j} in
            ((z * (z + 1)) `div` 2) + (if z == 0 then i else j)) + 1
-- i j 均从0开始
```

居然有通式啊!

要感谢玖


