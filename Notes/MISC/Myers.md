myers算法的关键是用动态规划去解决一个广度优先搜索的问题

```
       A     B     C     A     B     B     A

    o-----o-----o-----o-----o-----o-----o-----o   0
    |     |     | \   |     |     |     |     |
C   |     |     |  \  |     |     |     |     |
    |     |     |   \ |     |     |     |     |
    o-----o-----o-----o-----o-----o-----o-----o   1
    |     | \   |     |     | \   | \   |     |
B   |     |  \  |     |     |  \  |  \  |     |
    |     |   \ |     |     |   \ |   \ |     |
    o-----o-----o-----o-----o-----o-----o-----o   2
    | \   |     |     | \   |     |     | \   |
A   |  \  |     |     |  \  |     |     |  \  |
    |   \ |     |     |   \ |     |     |   \ |
    o-----o-----o-----o-----o-----o-----o-----o   3
    |     | \   |     |     | \   | \   |     |
B   |     |  \  |     |     |  \  |  \  |     |
    |     |   \ |     |     |   \ |   \ |     |
    o-----o-----o-----o-----o-----o-----o-----o   4
    | \   |     |     | \   |     |     | \   |
A   |  \  |     |     |  \  |     |     |  \  |
    |   \ |     |     |   \ |     |     |   \ |
    o-----o-----o-----o-----o-----o-----o-----o   5
    |     |     | \   |     |     |     |     |
C   |     |     |  \  |     |     |     |     |
    |     |     |   \ |     |     |     |     |
    o-----o-----o-----o-----o-----o-----o-----o   6

    0     1     2     3     4     5     6     7
```
