using TransactGuard.Client;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            using (CompensationScope compScope = new CompensationScope("", Guid.NewGuid())) 
            { 
            
            
            } 

        }
    }
}