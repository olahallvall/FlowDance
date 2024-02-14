using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

using TransactGuard.Client;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            string HotelServiceCompensationUrl = config.GetSection("MySettings").GetSection("CompensationUrls").GetSection("HotelService").Value;

            using (CompensationScope compScope = new CompensationScope(HotelServiceCompensationUrl, Guid.NewGuid())) 
            {

                compScope.Commit();
            } 
        }
    }
}