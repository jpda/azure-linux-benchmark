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
            //var r = new Reader();
            Reader.Load(@"D:\temp\bench\ub\195252-c3750a9e-4ac1-47e6-9d37-b2cd62d13f2e");
            Console.WriteLine("all finished");
            Console.ReadLine();
        }
    }

    public class Reader
    {
        public static void Load(string folderPath)
        {
            foreach (var f in new DirectoryInfo(folderPath).EnumerateFiles("*.log"))
            {
                Console.WriteLine($"found {f}, reading...");
                if (f.Name.IndexOf("CPU", StringComparison.Ordinal) <= -1) continue;

                var testData = File.ReadAllText(f.FullName);
                var tests = testData.Split(new[] { "start CPU iteration " }, StringSplitOptions.RemoveEmptyEntries);
                var cpuTests = new List<CpuTestResult>();
                foreach (var t in tests)
                {
                    
                    if (!t.Contains("sysbench")) continue;

                    var lines = t.Split('\n').ToList();
                    var iteration = lines.First();
                    var threads = lines.Single(x => x.StartsWith("Number of threads:")).Split(':')[1].Trim();
                    var maxPrime = lines.Single(x => x.StartsWith("Maximum prime number")).Split(':')[1].Trim();

                    var totalTime = lines.Single(x => x.Trim().StartsWith("total time:")).Split(':')[1].Trim();
                    var totalEvents = lines.Single(x => x.Trim().StartsWith("total number of events")).Split(':')[1].Trim();
                    var totalTimeByEvent = lines.Single(x => x.Trim().StartsWith("total time taken by event execution")).Split(':')[1].Trim();

                    var min = lines.Single(x => x.Trim().StartsWith("min:")).Split(':')[1].Trim();
                    var max = lines.Single(x => x.Trim().StartsWith("max:")).Split(':')[1].Trim();
                    var avg = lines.Single(x => x.Trim().StartsWith("avg:")).Split(':')[1].Trim();
                    var approx = lines.Single(x => x.Trim().StartsWith("approx.")).Split(':')[1].Trim();

                    Console.WriteLine($"--- start test iteration {iteration}, max prime {maxPrime} on {threads} threads ---");
                    Console.WriteLine($"Took {totalTime} to do {totalEvents}. Total processing time {totalTimeByEvent}.");
                    Console.WriteLine($"Min: {min}, Max: {max}, Avg: {avg}, 95%: {approx}");
                    Console.WriteLine($"--- end test iteration {iteration} ---");

                    var c = new CpuTestResult()
                    {
                        Threads = int.Parse(threads),
                        MaxPrime = int.Parse(maxPrime),
                        TotalTime = decimal.Parse(totalTime.Replace("s", "")) * 1000,
                        TotalEvents = int.Parse(totalEvents),
                        TotalEventTime = decimal.Parse(totalTimeByEvent) * 1000,
                        MinimumTime = decimal.Parse(min.Replace("ms", "")),
                        MaximumTime = decimal.Parse(max.Replace("ms", "")),
                        AverageTime = decimal.Parse(avg.Replace("ms", "")),
                        ApproximateTime = decimal.Parse(approx.Replace("ms", ""))
                    };
                    cpuTests.Add(c);
                }



            }
        }
    }

    public class CpuTestResult
    {
        public int Threads { get; set; }
        public int MaxPrime { get; set; }
        public decimal TotalTime { get; set; }
        public int TotalEvents { get; set; }
        public decimal TotalEventTime { get; set; }
        public decimal MinimumTime { get; set; }
        public decimal AverageTime { get; set; }
        public decimal MaximumTime { get; set; }
        public decimal ApproximateTime { get; set; }
    }
}