using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedisConsole
{
    public class RedLockUtitily
    {
        public static RedLockFactory GetRedlockFactory()
        {
            var endPoints = new List<RedLockEndPoint>();
            endPoints.Add(new DnsEndPoint("192.168.0.107", 6379));

            return RedLockFactory.Create(endPoints);
        }
    }
}
