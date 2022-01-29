## 简单的haskell生成简单音乐

原教程：https://m.youtube.com/watch?v=FYTZkE5BZ-0

b站搬运：https://b23.tv/BV1Kt4y197QL

所需工具：ffmpeg, ghc

### 声音

在讨论音乐之前，先让我们来讨论一下，什么是声音？

声音是一种纵波，在三维空间中是距离声源忽远忽近的振动，依赖物质传播，经常还和传播介质的温度有点关系，这段没让你回忆起什么就当我没说。

这是正弦波的声音:

https://eev.ee/media/2016-09-15-music/sine-wave.ogg 

波由频率、振幅和形状定义, 正弦波就是形状像sin(x)图像的波, 音乐家管波的形状叫 -- 音色。

人耳能听到的声音对频率有一定要求，一般是20 ~ 20000 Hz, 音乐家将频率称为音高(Pitch), 普通人可能更愿意称之为音调。

振幅影响听到的声音有多“响”，但是响是个主观感觉，所以还和人离声源的距离有关，距离越近，振幅越大，听到的响度越大。

不仅如此，即使振幅距离不变，只要将声音的频率调为原来的正整数倍 -- 注意不宜太大，就拿2倍当例子吧，你会觉得这还是原来的声音，只是音量调大了一些。

这是440Hz的正弦波:

https://eev.ee/media/2016-09-15-music/sine-wave.ogg

这是880Hz的正弦波:

https://eev.ee/media/2016-09-15-music/sine-wave-2.ogg

好吧，也没有那么像。

下面开始编程实现。

### 生成正弦波

第一个要搞定的问题是，如何在计算机中表示音波？一种可行的方法是用一组有顺序的浮点数来“模拟”正弦波，当然了，在haskell中，就用`[Float]`来表示。

```haskell
type Sound = [Float]

wave :: Sound
wave = map sin [0.0 .. 48000]
```

稍等一下，这样要怎么播放? 

我们要把[Float]转换成连续的字节流并写入文件，这样的音频文件称为raw file，暂时不能播放，但是用ffmpeg可以把raw file转换成wav文件，这样就可以播放了。

haskell中对应字节流的数据结构是ByteString。让我们来看看怎么做比较好。


### Builder

这个不是设计模式里面那个Builder，是bytestring模块给的一个辅助类型, 模块是`Data.ByteString.Builder`。

让我们看看文档怎么说的：

> Builders are used to efficiently construct sequences of bytes from smaller parts. Typically, such a construction is part of the implementation of an encoding, i.e., a function for converting Haskell values to sequences of bytes. 

> Builders support (a) by providing an O(1) concatentation operation and efficient implementations of basic encodings for Chars, Ints, and other standard Haskell values. They support (b) by providing their result as a lazy ByteString, which is internally just a linked list of pointers to chunks of consecutive raw memory.

简短截说：Builder是一种函数，如果用它来把一些零散的haskell里面的值转换成ByteString会很高效。Builder类型有Monoid实例，mappend操作的时间复杂度是常数。

首先要做的事是把float转换为一个builder,这一步需要考虑一个叫字节序的东西，简短截说，Intel CPU用户用floatLE函数。

下一步，把`[Builder]`变成Builder，拼接操作使用mconcat或者fold都可以，为方便就用fold了。

然后用`toLazyByteString`函数将Builder转换为惰性的ByteString，此处用Lazy的原因不做说明(上面文档写了)

最后写入文件，用`writeFile`即可。

```haskell
import qualified Data.ByteString.Lazy as B
import qualified Data.ByteString.Builder as B
import Data.Foldable (fold)

save :: FilePath -> IO ()
save path =  B.writeFile path $ B.toLazyByteString $ fold $ map B.floatLE wave
```

### 转换

该到ffmpeg上场了，假设字节流被写入`output.raw`, 那么

```bash
ffmpeg -f f32le -ar 48000 -i output.raw output.wav
```

`-ar 48000`的意思是48000个样本点(一个float代表一个样本点)作为一秒，考虑到wave的定义，也就是说我们生成了一个长度一秒的wav文件。如果需要给它一个名字，不妨叫它`sampleRate`。

好像什么地方不对，让我们来算笔账：

sin(x)的最小正周期为2π, 那么我们生成的声音的频率就是48000 / 2π, 以3.1415926为估计值计算

```scheme
scheme@(guile-user)> (/ 48000 (* 2 pi))
$2 = 7639.437398725729
```

......好像是那个，高了。在笔者的红米note8上只能听见尖锐的蜂鸣器响声。

不能再用wave了，让我们来实现一个函数`freq`，根据输入的时间和频率生成对应的正弦波。


```haskell
type Sound = [Pulse]
type Seconds = Float
type Hz = Float
type Pulse = Float



sampleRate :: Float
sampleRate = 48000.0

freq :: Hz -> Seconds -> Sound
freq hz duration =
  map (sin . (* step)) [0.0 .. sampleRate * duration] where
    step = (hz * 2 * pi) / sampleRate
-- Prelude.pi 
```

好耶! 走向模块化! 

### 十二平均律

程序员大概有不少知道C#, 这个语言的名字来源于西方音乐中的升C大调。要多了解些有关乐理的东西，可以看这个：

https://segmentfault.com/a/1190000015967252

坦白地说，笔者没有耐心把上面那篇文章看完，省流模式：西方音乐使用的音高(频率)有12个，自440Hz起，以880Hz终，成等比数列，比值约为1.0594631。

坦白说如此简化的模型能解释的东西很少，但是这只是一篇“博客文章”, 而且主要来源是一篇视频教程，好在现代编程可以不用懂原理就能编写简单的功能性程序。

### Envelop

https://zhuanlan.zhihu.com/p/38619552

太长不看版：乐器发出的声音不可能像计算机生成的一样开头结尾持续一个频率，应该有个Attack(起音)、Decay(衰减)、Sustain(延持)、与Release(释音)的过程，不同乐器的过程曲线不一样。这样的话现在的freq就有点不够用了。

原作者到这就果断鸽了，弄了个简单的Attack Sustain然后就没管太多了。

### Final

懒得写了，人生总是处在拉垮和更拉垮之间。
原作者最后弄出的音乐听着还不错，不过若你有想法要开始用编程方式创作音乐，比较好的选择有：

+ [Sonic Pi](https://sonic-pi.net/)

+ [Overtone](http://overtone.github.io/docs.html), 由clojure编写。

haskell有本音乐编程的书，但库已经年久失修, 只可意会。

使用正弦波的原因我猜是因为任意波形的音波可表示为一组正弦波的加权和。通过一种叫做傅立叶变换的数学技巧，可以从音波中提取出对应的一组正弦波。
