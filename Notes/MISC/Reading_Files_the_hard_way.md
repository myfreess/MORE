原文：https://fasterthanli.me/series/reading-files-the-hard-way

# Part 1

介绍了一下nodejs，C，rust的文件读取API，并使用strace追踪它们使用的linux syscall(特定平台的实现特定细节)

> Node.js, Rust, and C applications all end up using the same few functions: open(), read(), write(), etc.

# Part 2

通过gdb，发现以上的文件读取功能都是使用glibc间接进行syscall。

```c
// in `glibc/sysdeps/unix/sysv/linux/read.c`

/* Read NBYTES into BUF from FD.  Return the number read or -1.  */
ssize_t
__libc_read (int fd, void *buf, size_t nbytes)
{
  return SYSCALL_CANCEL (read, fd, buf, nbytes);
}
libc_hidden_def (__libc_read)

libc_hidden_def (__read)
weak_alias (__libc_read, __read)
libc_hidden_def (read)
weak_alias (__libc_read, read)
```

在x86平台上，内核运行在ring 0，应用运行在ring3

When we first mmap a file, the kernel might eagerly read the first 4K of the file into a buffer of its own, and sets up the page tables so that the (userland) process can read directly from that buffer

# Part 3

## In the belly of the beast

使用ftrace追踪内核的内部机制

## Where did our paths go?

符号链接相关

symlinks are just files, with a special mode, and their contents is a path

## You can make a filesystem out of that (or can you?)

What's a filesystem? Just a way to organize file and folders, their contents, and their metadata on disk. 

we divide this disk in a series of blocks. Let's make them 4096 bytes (or 4KiB). That way, they might be the same size as kernel memory pages. Or a multiple. At least they're a power of two.

## Enter inodes

Each of those inodes will contain the usual, but also, it'll contain the number of the block we can find its data in.

Right now, traversing downwards is efficient (more than before), but traversing upwards (going from /etc to /, for example) still requires manipulating paths, and we don't want to do that.

Here's an idea: what if we add an entry to each directory, that points to its parent? We could call it "..". And for the root, we'll just make it point to itself

## Solving the remaining problems

+ Right now, every directory entry is stored in no particular order, in a list. To find a specific entry, we have to do a linear search, ie. do a comparison with each entry one after the other, and stop only when we've found what we've looking for. (A potential solution is to hash the names of all the entries, and use a Self-balancing binary search tree.)
+ The second problem is that our files still have to be contiguous. If they grow, we might have to move them to another set of blocks. We might even find that there is no contiguous set of blocks large enough to hold the file!

To address that, we can use extents. For each file, we can store a series of (start, length) pairs:

This brings on a new problem: previously, if we wanted to read just the second half of a file, we could simply calculate the address of the first block, But now, the middle of the file may be in any of the extents. It's not simple arithmetic anymore. To remedy this, we can also use a tree data structure.

## Now for a taste of the real world

We can find out what filesystem it is with the df command (specifically, the -T flag)

https://www.kernel.org/doc/html/latest/filesystems/ext4/overview.html

## Let's read a whole partition I guess

通过df拿到的设备文件名(例如`/dev/sda3`)可以用于读取原始的磁盘数据

For the special case of block group 0, the first 1024 bytes are unused, to allow for the installation of x86 boot sectors and other oddities. The superblock will start at offset 1024 bytes, whichever block that happens to be (usually 0).

The docs mention a "superblock", and if we read up on its structure, it says that we should find the magic number 0xEF53 at offset 0x38. It also says it's a little-endian 16-bit integer.

## Block groups

An ext4 file system is split into a series of block groups. To reduce performance difficulties due to fragmentation, the block allocator tries very hard to keep each file's blocks within the same group, thereby reducing seek times. The size of a block group is specified in sb.s_blocks_per_group blocks, though it can also calculated as 8 * block_size_in_bytes. With the default block size of 4KiB, each group will contain 32,768 blocks, for a length of 128MiB. The number of block groups is the size of the device divided by the size of a block group.


