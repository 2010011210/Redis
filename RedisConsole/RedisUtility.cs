using RedisConsole.Model;
using ServiceStack.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RedisConsole
{
    public class RedisUtility
    {
        public static void Show() 
        {
            // public RedisClient(string host, int port, string password = null, long db = 0L),默认是0库，如果想要指定到特定的库，添加第4个参数
            using (RedisClient client = new RedisClient("127.0.0.1", 6379, null, 1))  
            {
                client.FlushDb();  // 删除当前数据库的数据，默认是0
                //client.FlushAll(); //删除所有数据

                var addResult = client.Add<string>("Car", "BYD");
                addResult = client.Add("Car", "BYD YuanPlus");  // 如果Car存在，添加失败。 如果要更新Car，用Set

                var result = client.Set<string>("Car", "元Plus", TimeSpan.FromSeconds(60)); // 过期日期不填，就是永久有效

                var result2 = client.Set("Car2", "BYD");
                client.Expire("Car2", 30);

                var getResult = client.GetValue("Car"); //这个是  "元Plus" 因为值都是序列化之后保存进去的，取出来要反序列化一下
                Console.WriteLine(getResult);
                var getResult2 = client.Get<string>("Car"); // 这个是  元Plus， 不带括号 。这个方法会自动反序列化，和原来的值一样。
                Console.WriteLine(getResult2);

                #region 多个值同事赋值和取值
                client.SetAll(new Dictionary<string, string> { { "id", "410882" }, { "Province", "河南" } });
                List<string> list = new List<string>() { "id", "Province" };
                var values = client.GetAll<string>(list.ToArray());

                foreach (var item in values)
                {
                    Console.WriteLine($"{item.Key}:{item.Value}");
                }

                #endregion

                #region 秒杀

                var expiredResult = client.Set<int>("number", 1000, TimeSpan.FromDays(2));
                var expiredResult2 = client.Get<int>("number");

                for (int i = 0; i < 100; i ++) 
                {
                    expiredResult2 = client.Get<int>("number");
                    Console.WriteLine(expiredResult2);
                    var resultNumber = client.Decr("number");  //减去
                    //resultNumber = client.DecrBy("number",10);  //减去10
                    //client.Incr("number"); // 加1
                }
                #endregion

            }
        }

        #region 秒杀

        public static void SecondKill() 
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                #region 秒杀
                var expiredResult = client.Set<int>("number", 300, TimeSpan.FromDays(2));
                #endregion
            }

            List<Task> tasks = new List<Task>();
            for (int i =0; i < 301; i++) 
            {
                //tasks.Add(Task.Run(() => { Work(); }));
                tasks.Add(Task.Run(() => { WorkWithLua(); }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine( "抢票结束");
        }

        public static void Work() 
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                var resultNumber = client.Decr("number");  //减去
                if (resultNumber > 0)
                {
                    Console.WriteLine($"{resultNumber};抢票成功");
                }
                else 
                {
                    Console.WriteLine($"{resultNumber};抢票失败");
                }
            }
        }

        public static void WorkWithLua()
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                string lua = @" local count = redis.call('get',KEYS[1])
                                if(tonumber(count) > 0)
                                then
                                    redis.call('INCR',ARGV[1])
                                    return redis.call('DECR',KEYS[1])
                                else
                                    return tonumber(count)
                                end
                              ";
                try 
                {
                    var result = client.ExecLuaAsInt(lua, keys: new string[] { "number" }, args: new string[] { "orderNumber" });
                    if (result > -1)
                    {
                        Console.WriteLine($"秒杀成功，剩余数量{result}");
                    }
                    else 
                    {
                        Console.WriteLine($"秒杀失败，剩余数量{result}");
                    }
                }
                catch (Exception e) 
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static void WorkWithLock(string key)
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                using (var dataLock = client.AcquireLock("LockKey:" + key, TimeSpan.FromSeconds(30))) 
                {
                
                }
            }
        }

        #endregion

        #region 哈希类型
        /// <summary>
        /// 哈希值
        /// </summary>
        public static void HashTable()
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379, null, 1))
            {
                string hashId = "wangcongFamily";
                var hashAddResult = client.SetEntryInHash(hashId, "name", "wangcong2");  // 命令行是这样写 hset hashId name wangcong2
                hashAddResult = client.SetEntryInHash(hashId, "age", "23");

                var name = client.GetValueFromHash(hashId, "name");                     // 等同于命令行 hget hashId name
                var name2 = client.GetValuesFromHash(hashId, new string[] { "name", "age"});

                // 批量操作
                Dictionary<string, string> worker = new Dictionary<string, string>();
                worker.Add("name", "duanshan");
                worker.Add("age", "34");
                client.SetRangeInHash(hashId, worker);    // 等同于命令行 HMSet hashId  name duanshan1  age 35

                var dic = client.GetAllEntriesFromHash(hashId); // 等同于 HGetAll hashId
                foreach (var item in dic) 
                {
                    Console.WriteLine( $"{item.Key}:{item.Value}");
                }

                // 获取所有的key
                var keys = client.GetHashKeys(hashId);   // 命令行等同于 HKeys hashId

                // 一个对象保存到hashTable，对象必须有id值
                var user = new User() { Id = "001", Name = "大头", Age = "33" };  //必须要有Id值，否则会自动生成一串哈希值
                var user2 = new User() { Id = "002", Name = "三丫头", Age = "34" };
                client.StoreAsHash<User>(user);
                client.StoreAsHash<User>(user2);

                var userData = client.GetFromHash<User>("001");
                // 删除值
                client.RemoveEntryFromHash(hashId, "age");   // 命令行等同于 HDel hashId age
            }
        }

        #endregion

        #region List
        /// <summary>
        /// 哈希值
        /// </summary>
        public static void RedisList()
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                string hashId = "wangcongFamily";
                client.AddItemToList("shu", "刘备");  // LPUSH shu 刘备
                client.AddItemToList("shu", "关羽");

                client.PushItemToList("shu", "张飞");   // 后面追加
                client.PrependItemToList("shu", "诸葛亮");  // 前面追加 RPush shu 诸葛亮

                // 批量操作
                client.AddRangeToList("wei", new List<string>() { "曹操", "司马懿", "张辽"});  // LPUSH wei 曹操 司马懿 张辽
                // 按下标查询
                var people = client.GetItemFromList("wei", 0);    // 等同于命令行  LIndex wei 0
                var peoples = client.GetRangeFromList("wei", 0,1); // 等同于命令行 LRange wei 0 1

                // 获取所有的
                peoples = client.GetAllItemsFromList("wei");

                var lastItem = client.RemoveEndFromList("shu"); // 移除尾部  LPOP shu
                var firstItem = client.RemoveStartFromList("shu"); // 移除尾部 RPOP shu

                client.SetItemInList("wei", 1, "司马昭之心，路人皆知"); // 等同于 LSet wei 1 changByCommondLine

                client.AddItemToList("wu","孙权");
                client.Expire("wu", 20);  //设置过期时间 Expire wu 20
            }
        }

        #endregion

        #region Set 去重，并集，差集，补集
        /// <summary>
        /// 哈希值
        /// </summary>
        public static void RedisSet()
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                client.AddItemToSet("投票_赞成", "中国");  // SADD key value [value...]
                client.AddItemToSet("投票_赞成", "中国");
                client.AddItemToSet("投票_赞成", "俄罗斯");
                client.AddItemToSet("投票_赞成", "法国");

                long yesCount = client.GetSetCount("投票_赞成");  // SCARD 投票_反对
                Console.WriteLine($"赞成个数：{yesCount}");

                client.AddItemToSet("投票_反对", "美国");
                client.AddItemToSet("投票_反对", "英国");
                client.AddItemToSet("投票_反对", "美国");
                client.AddItemToSet("投票_反对", "法国");

                long noCount = client.GetSetCount("投票_反对");  // SCARD 投票_反对
                Console.WriteLine($"反对个数：{noCount}");

                var yesItems = client.GetAllItemsFromSet("投票_赞成");   // SMEMBERS key //获取所有成员
                Console.WriteLine($"赞成的人员：{string.Join(",", yesItems)}");

                // 批量添加
                client.AddRangeToSet("投票_赞成", new List<string>() { "古巴", "曹县", "越南", "古巴"} );
                Console.WriteLine($"赞成个数：{client.GetSetCount("投票_赞成")}");

                // 移除 
                client.RemoveItemFromSet("投票_赞成","古巴");

                // 从set中随机返回元素,从set中返回并移除
                for (int i = 0; i < 6; i++) 
                {
                    var popItem = client.PopItemFromSet("投票_赞成"); // SPOP key [count]
                    Console.WriteLine($"删除：{popItem},还有{string.Join(",",client.GetAllItemsFromSet("投票_赞成"))}");
                }

                // 交集
                client.AddRangeToSet("投票_赞成", new List<string>() { "中国", "俄罗斯", "法国", "古巴", "曹县", "越南", "古巴" });
                client.AddRangeToSet("投票_反对", new List<string>() { "美国", "英国", "法国" });
                var allHave = client.GetIntersectFromSets("投票_赞成", "投票_反对");  // 交集 SINTER key [key...]
                var mergeSet = client.GetUnionFromSets("投票_赞成", "投票_反对");     // 并集

                client.Expire("投票_赞成", 60);
            }
        }


        #endregion

        #region ZSet

        public static void RedisZSet()
        {
            using (RedisClient client = new RedisClient("127.0.0.1", 6379))
            {
                string zsetKey = "GDPRank";
                string zsetKeyAsia = "AsiaGDPRank";
                client.RemoveRangeFromSortedSet(zsetKey, 0, 10);
                client.AddItemToSortedSet(zsetKey, "Japan", 5);  //  先比较socre，score相同，根据set内容的字母排序
                client.AddItemToSortedSet(zsetKey, "UK", 4);  //   ZADD GDPRank 2 Vitnan
                client.AddItemToSortedSet(zsetKey, "France", 4);  // 
                client.AddItemToSortedSet(zsetKey, "France4", 4);  // 
                client.AddItemToSortedSet(zsetKey, "France1", 4);  // 
                client.AddItemToSortedSet(zsetKey, "France3", 4);  // 
                client.AddItemToSortedSet(zsetKey, "France2", 4);  // 
                client.AddItemToSortedSet(zsetKey, "Germany", 4);  // 
                client.AddItemToSortedSet(zsetKey, "China", 20);  // 
                client.AddItemToSortedSet(zsetKey, "USA", 27);  // 
                client.AddItemToSortedSet(zsetKey, "Indian", 3);  // 

                var values = client.GetAllItemsFromSortedSet(zsetKey);
                Console.WriteLine($"正序：{string.Join(",", values)}");

                var valuesDesc = client.GetAllItemsFromSortedSetDesc(zsetKey);
                Console.WriteLine($"倒序：{string.Join(",", valuesDesc)}");

                long yesCount = client.GetSetCount("投票_赞成");
                Console.WriteLine($"赞成个数：{yesCount}");

                client.AddItemToSortedSet(zsetKeyAsia, "Japan", 5);
                client.AddItemToSortedSet(zsetKeyAsia, "China", 20);
                client.AddItemToSortedSet(zsetKeyAsia, "Indian", 3);  //
                client.AddItemToSortedSet(zsetKeyAsia, "Korea", 2);  

                client.StoreIntersectFromSortedSets("AisaGDPInTop10",zsetKey, zsetKeyAsia); //亚洲GDP前5排在世界前10的国家，注意，合并得到的score会相加

                // ZScore GDPRank   China   获取China的socre 
                // ZINCRBY GDPRank 10  China   China的socre 增加10

                // 获取前5名 ZRange GDPRank 0 5
                // 倒序前5名 ZRevRange GDPRank 0 5
            }
        }

        #endregion


    }
}
