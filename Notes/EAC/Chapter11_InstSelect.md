将IR倒腾到CPU指令的过程称为指令选择

常见的两种方式

+ 通过窥孔优化
+ 通过树匹配

## 11.1 简介

上述的两种方法都有工具根据目标机器的描述信息自动生成指令选择器

指令选择器输出的可能不是合法汇编，例如仍然包含无上限的虚拟寄存器，需要寄存器分配

对目标机器和ISA的描述应该包含： register-set sizes; a description of each operation; the number, capabilities, and operation latencies of the functional units; memory alignment restrictions; and the procedure-call convention. 

## 11.2 背景

GCC的寄存器转移语言比ISA更不抽象，需要把多个IR操作合成为一条机器指令

处理器提供多种方式完成同一个IR操作，每种方式的成本与限制不同

### 11.2.1 ISA设计对选择的影响

两大选择器分支增多因素

+ 完成同一任务的多种机制
+ 地址模式和算术操作的增殖

#### 重复的实现

举了个寄存器到寄存器复制数据的例子

#### 地址模式

现实中的ISA属实选择有点太多，操作的长度和机器周期数都不一样

对于分支跳转指令，选用何种地址模式跟源地址到终点的距离和方向等多个因素都有关系，需要格外小心

##### 抽象层级

##### 寄存器使用

##### 成本

### 11.2.2 例子

## 11.3 窥孔优化

起源于1960s - 1970s

### 11.3.1 窥孔

窗口扫过代码，对特定模式识别并化简

早期 窗口小 模式硬编码

现在三阶段

+ 展开：IR -> Low Level IR
+ 简化：Low Level IR -> Low Level IR
+ 匹配：Low Level IR -> ASM

### 11.3.2 简化器

