# Redis
Redis
#常用的集中数据结构 
1. string字符串
2. Hash 哈希
3. List 集合
4. Set 去重的集合
5. ZSet 自带去重和排序的集合
6. BitMaps(布隆过滤器) 是字符串类型，是个位符的二进制组成
7. HyperLogLoss Set的升级版本，提供一个不太准确的基数统计。
8. GEO 地理坐标
9. Streams 流， kafka是借助流服务开发---他是内存版kafka--  

# 1.string类型
可以使用 help @string查看命令行有哪些

设置值和过期时间。 
~~~
set key value  expiredTime 
例如：
set name Tom   设置name的值是Tom，

// 设置过期时间，精确到秒
SET myKey "myValue"  
EXPIRE myKey 60  # 设置myKey的过期时间为60秒

上面两行代码可以合并为一行
SETEX myKey 60 "myValue"  # 设置myKey的值为myValue，并设置过期时间为60秒

///如果过期时间精确到毫秒
SET myKey "myValue"  
PEXPIRE myKey 60000  # 设置myKey的过期时间为60000毫秒（即60秒）
上面两行代码可以合并为一行
PSETEX myKey 60000 "myValue"  # 设置myKey的值为myValue，并设置过期时间为60000毫秒（即60秒）

setnx key value //当key不存在才赋值
incr key  //key是值类型，每次增加1
incrby key 100  // key是值类型，每次增加100

MSET key value [key value ...] //一次插入多条数据

~~~

# 2.hash哈希类型 
可以存放对象类型的结构

# 3.list类型 
可以实现队列的功能，先进先出，先进后出

# 4. set类型
主要功能是可以去重 

