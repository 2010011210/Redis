using RedLockNet.SERedis;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisConsole
{
    public class RedisUtitily
    {
        public static string RedisServerIp = "192.168.0.107";
        public static int RedisServerPort = 6379;

        public static object LockObject = new object();
        public static string keylock = "redislock";

        public static RedLockFactory redLockFactory = RedLockUtitily.GetRedlockFactory();


        public static RedisClient GetClient()
        {
            return new RedisClient(RedisServerIp, RedisServerPort);
        }
        public static void Test()
        {
            var lockValue = Guid.NewGuid().ToString() + Thread.CurrentThread.ManagedThreadId;
            //var rLock = redLockFactory.CreateLock(keylock, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(20));
            try
            {
                using (RedisClient client = GetClient())
                {
                    var flag = client.SetValueIfNotExists(keylock, lockValue, TimeSpan.FromSeconds(60));
                    if (flag)
                    {
                        //var a = redisClient.Get("test123");
                        var count = client.Get<int>("good:001");
                        if (count <= 0)
                        {
                            Console.WriteLine("good:001商品已经卖光");
                        }
                        else
                        {
                            client.Decr("good:001");
                            Console.WriteLine($"第{count}件商品被购买");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
            }
            finally
            {
                using (RedisClient client = GetClient())
                {
                    try
                    {
                        var lua = $"if redis.call(\"get\", KEYS[1]) == ARGV[1] then" +
                                "  return redis.call(\"del\", KEYS[1])" +
                              "  else" +
                                "  return 0" +
                               "  end";

                        int res = (int)client.ExecLuaAsInt(lua, keys: new string[] { keylock }, args: new string[] { lockValue });
                        if (res == 1)
                        {
                            Console.WriteLine($"删除key成功{lockValue}");
                        }
                        else
                        {
                            Console.WriteLine($"删除key失败{lockValue}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"error:{ex.Message}");
                    }
                }
                //rLock.Dispose();
                //redLockFactory.Dispose();

            }

        }

        public static void TestRedLock()
        {
            var lockValue = Guid.NewGuid().ToString() + Thread.CurrentThread.ManagedThreadId;　　　　　　　　//expire 锁过期时间　　　　　　　　//wait 线程等待时间，如果更呆了wait时间还没有获得锁，放弃　　　　　　　　//retry //每隔多长时间试着获取锁
            var rLock = redLockFactory.CreateLock(keylock, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(20));
            try
            {
                using (RedisClient client = GetClient())
                {
                    if (rLock.IsAcquired)
                    {
                        //var a = redisClient.Get("test123");
                        var count = client.Get<int>("good:001");
                        if (count <= 0)
                        {
                            Console.WriteLine("good:001商品已经卖光");
                        }
                        else
                        {
                            client.Decr("good:001");
                            Console.WriteLine($"第{count}件商品被购买");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
            }
            finally
            {
                rLock.Dispose();
                Console.WriteLine($"finally {lockValue}");
            }

        }
    }
}