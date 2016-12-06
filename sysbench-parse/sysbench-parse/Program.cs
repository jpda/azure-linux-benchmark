using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sysbench_parse
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class Reader
    {
        public static void Load(string folderPath)
        {
            foreach (var f in new DirectoryInfo(folderPath).EnumerateFiles("*.log"))
            {
                Console.WriteLine($"found {f}, reading...");
                if (f.Name.IndexOf("CPU", StringComparison.Ordinal) > -1)
                {
                    var testData = File.ReadAllText(f.FullName);
                    var tests = testData.Split("iteration".ToCharArray());

                    //cpu tests
                    var lines = File.ReadAllLines(f.FullName).Select(x => x.ToLower()).ToList();
                    var threadLine = lines.Where(x => x.StartsWith("number of threads: "));
                    if (threadLine.Any())
                    {
                        
                    }
                }
            }
        }
    }
}