using System;
using System.Collections.Generic;
using System.Globalization;
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
            Reader.Load(@"E:\temp\azbench\193820-90437862-7910-4d1d-b45f-1b00a8883650");
            Console.WriteLine("all finished");
            Console.ReadLine();
        }
    }

    public class Reader
    {
        public static void Load(string folderPath)
        {
            var results = new List<Test>();
            foreach (var f in new DirectoryInfo(folderPath).EnumerateFiles("*.log"))
            {
                Console.WriteLine($"found {f}, reading...");
                if (f.Name.StartsWith("CPU"))
                {
                    var c = new CPUTest(f.FullName);
                    results.Add(c);
                }
                if (f.Name.StartsWith("DISK-"))
                {
                    var d = new DiskTest(f.FullName);
                    results.Add(d);
                }
            }
            foreach (var r in results)
            {
                Console.WriteLine($" --- Got result {r.GetType()} --- ");
                foreach (var i in r.Results)
                {
                    foreach (var p in i.GetType().GetProperties())
                    {
                        Console.WriteLine($"{p.Name}: {p.GetValue(i)}");
                    }
                }
            }
        }
    }

    public class Test
    {
        public List<TestIteration> Results { get; set; }

        public Test()
        {
            Results = new List<TestIteration>();
        }
    }

    public class CPUTest : Test
    {
        public CPUTest(string fileName)
        {
            var testData = File.ReadAllText(fileName);
            var tests = testData.Split(new[] { "START CPU iteration " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tests)
            {
                if (!t.Contains("sysbench")) continue;
                var lines = t.Split('\n').ToList();
                var c = new CPUTestIteration(lines);
                Results.Add(c);
            }
        }
    }

    public class DiskTest : Test
    {
        public DiskTest(string fileName)
        {
            var testData = File.ReadAllText(fileName);
            var tests = testData.Split(new[] { "START DISK iteration " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tests)
            {
                if (!t.Contains("sysbench")) continue;
                var lines = t.Split('\n').ToList();
                var c = new DiskTestIteration(lines);
                Results.Add(c);
            }
        }
    }

    public class TestIteration
    {
        public DateTime TestStartTime { get; set; }
        public int Threads { get; set; }
        public int Iteration { get; set; }
        public decimal TotalTime { get; set; }
        public int TotalEvents { get; set; }
        public decimal TotalEventTime { get; set; }
        public decimal MinimumTime { get; set; }
        public decimal AverageTime { get; set; }
        public decimal MaximumTime { get; set; }
        public decimal ApproximateTime { get; set; }
        public decimal RequestRate => TotalEvents / TotalTime;

        protected TestIteration(List<string> lines)
        {
            ProcessStandardInfo(lines);
        }

        public void ProcessStandardInfo(List<string> lines)
        {
            Iteration = int.Parse(lines.First().Split('|')[0].Trim().Split(' ').Last());
            //Thu Dec  8 19:39:17 UTC 2016
            var date = lines.First().Split('|')[1].Replace("UTC", "");
            var datePieces = date.Split(' ').Where(x => x.Trim() != string.Empty).ToList();
            //fix date + hour components if need leading zeros
            datePieces[2] = datePieces[2].Length > 1 ? datePieces[2] : "0" + datePieces[2];
            datePieces[3] = datePieces[3].Length > 1 ? datePieces[3] : "0" + datePieces[3];
            var cleanDate = string.Join(" ", datePieces);
            TestStartTime = DateTime.ParseExact(cleanDate, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);

            Threads = int.Parse(lines.Single(x => x.StartsWith("Number of threads:")).Split(':')[1].Trim());
            TotalTime = decimal.Parse(lines.Single(x => x.Trim().StartsWith("total time:")).Split(':')[1].Trim().Replace("s", "")) * 1000;
            TotalEvents = int.Parse(lines.Single(x => x.Trim().StartsWith("total number of events")).Split(':')[1].Trim());
            TotalEventTime = decimal.Parse(lines.Single(x => x.Trim().StartsWith("total time taken by event execution")).Split(':')[1].Trim()) * 1000;

            MinimumTime = decimal.Parse(lines.Single(x => x.Trim().StartsWith("min:")).Split(':')[1].Trim().Replace("ms", ""));
            MaximumTime = decimal.Parse(lines.Single(x => x.Trim().StartsWith("max:")).Split(':')[1].Trim().Replace("ms", ""));
            AverageTime = decimal.Parse(lines.Single(x => x.Trim().StartsWith("avg:")).Split(':')[1].Trim().Replace("ms", ""));
            ApproximateTime = decimal.Parse(lines.Single(x => x.Trim().StartsWith("approx.")).Split(':')[1].Trim().Replace("ms", ""));
        }
    }

    public class DiskTestIteration : TestIteration
    {
        public int ReadOps { get; set; }
        public int WriteOps { get; set; }
        public int OtherOps { get; set; }
        public int TotalOps => ReadOps + WriteOps + OtherOps;
        public decimal DataReadInMb { get; set; }
        public decimal DataWrittenInMb { get; set; }
        public decimal TotalTransferred => DataReadInMb + DataWrittenInMb;
        public decimal TransferRateInMbS => TotalTransferred / TotalTime;

        public DiskTestIteration(List<string> lines) : base(lines)
        {
            var ops = lines.Single(x => x.StartsWith("Operations performed:"));
            var split = ops.Split(':')[1].Split(',');
            ReadOps = int.Parse(split[0].Trim().Split(' ')[0]);
            WriteOps = int.Parse(split[1].Trim().Split(' ')[0]);
            OtherOps = int.Parse(split[2].Trim().Split(' ')[0]);

            var trans = lines.Single(x => x.StartsWith("Read "));
            var transPieces = trans.Split(' ').Where(x => x.Trim() != string.Empty).ToList();

            var read = transPieces[1];
            var readUnit = new string(read.Skip(read.Length - 2).ToArray());
            DataReadInMb = decimal.Parse(read.Replace(readUnit, ""));
            if (readUnit == "Gb")
            {
                DataReadInMb = DataReadInMb * 1024;
            }

            var written = transPieces[1];
            var writtenUnit = new string(written.Skip(written.Length - 2).ToArray());
            DataWrittenInMb = decimal.Parse(written.Replace(writtenUnit, ""));
            if (writtenUnit == "Gb")
            {
                DataWrittenInMb = DataWrittenInMb * 1024;
            }
        }
    }

    public class CPUTestIteration : TestIteration
    {
        public int MaxPrime { get; set; }

        public CPUTestIteration(List<string> lines) : base(lines)
        {
            MaxPrime = int.Parse(lines.Single(x => x.StartsWith("Maximum prime number")).Split(':')[1].Trim());
        }
    }
}