using System.Collections;

namespace RedisConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            //RedisUtility.RedisList();
            //RedisUtility.Show(); 
            //RedisUtility.SecondKill();
            //RedisUtility.RedisSet(); //
            RedisUtility.RedisZSet(); 
            #region string

            #endregion
        }
    }
}
