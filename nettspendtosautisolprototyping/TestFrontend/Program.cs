using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace TestFrontend
{
    public class Program
    {
        private static readonly HttpClient Client = new HttpClient();

        public static async Task Main(string[] args)
        {
           Expander expander = new Expander();
           expander.Expand("Drake", 3);
           expander.PrintNetwork();
        }
    }
}
