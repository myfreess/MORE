# Introduction

Why building distributed systems

Some applications are inherently distributed

Another reason for building distributed systems is that some appli-
cations require high availability and need to be resilient to single-
node failures

Some applications need to tackle workloads that are just too big to
fit on a single node

And finally, some applications have performance requirements
that would be physically impossible to achieve with a single
node. 

## Communication

it would be convenient to assume that some networking
library is going to abstract all communication concerns away, in
practice it’s not that simple because abstractions leak

## Coordination

A fault is a component that stopped working, and a system is
fault-tolerant when it can continue to operate despite one or more
faults. 

## Scalability

The performance of a distributed system represents how efficiently
it handles load, and it’s generally measured with throughput and
response time. Throughput is the number of operations processed
per second, and response time is the total time between a client
request and its response.

负载根据系统用途有很多种定义，如

+ 并发用户数
+ 连接数
+ 读写比值

增大系统承载能力可以买好硬件，也可以多加几台机子

## Resiliency

A distributed system is resilient when it can continue to do its job
even when failures happen

Failures that are left unchecked can impact the system’s availability,
which is defined as the amount of time the application can serve
requests divided by the duration of the period measured.

## Operations

DevOps, 微服务

# Reliable links

## Reliability

分组，编号，ACK, 定时重传, checksum

## Connection lifecycle

A connection needs to be opened before any data can be transmit-
ted on a TCP channel. 