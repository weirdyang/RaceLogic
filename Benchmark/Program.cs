﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LiteDB;
using maxbl4.Race.Logic;
using maxbl4.Race.Logic.RoundTiming;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Specify benchmark name: track, liteId");
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "track":
                    Track(args.Skip(1).ToArray());
                    break;
                case "liteid":
                    LiteId(args.Skip(1).ToArray());
                    break;
            }
        }

        static void LiteId(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("LiteDb complex id insertion  benchmark. Supply number of items to insert");
                Console.WriteLine("Example: benchmark.exe liteId 10000");
                return;
            }

            var count = int.Parse(args[0]);
            var longId = 1L;
            var random = new Random(1234);
            
            
            DoBenchmark("Fast", () => new LiteEntityLong{ Id = longId++, Address = "some address long", Amount = random.Next(), PersonName = "some person name"});
            DoBenchmark("Fast", () => new LiteEntityGuid{ Id = Guid.NewGuid(), Address = "some address long", Amount = random.Next(), PersonName = "some person name"});
            DoBenchmark("Fast", () => new LiteEntityId{ Id = Id<LiteEntityId>.NewId(), Address = "some address long", Amount = random.Next(), PersonName = "some person name"});
            // DoBenchmark("Bogus", () => longFaker.Generate());
            // DoBenchmark("Bogus", () => guidFaker.Generate());
            // DoBenchmark("Bogus", () => idFaker.Generate());
            BsonMapper.Global.RegisterType(x => x.Value.ToString("N"), x => new Id<LiteEntityId>(Guid.Parse(x)));
            DoBenchmark("String Id Fast", () => new LiteEntityId{ Id = Id<LiteEntityId>.NewId(), Address = "some address long", Amount = random.Next(), PersonName = "some person name"});
            //DoBenchmark("String Id Bogus", () => idFaker.Generate());
            BsonMapper.Global.RegisterType(x => x.Value, x => new Id<LiteEntityId>(x));
            DoBenchmark("Guid Id Fast", () => new LiteEntityId{ Id = Id<LiteEntityId>.NewId(), Address = "some address long", Amount = random.Next(), PersonName = "some person name"});
            //DoBenchmark("Guid Id Bogus", () => idFaker.Generate());
            

            void DoBenchmark<T>(string name, Func<T> faker) where T : class
            {
                var storageFile = faker.GetType().GenericTypeArguments[0].Name + ".litedb";
                if (File.Exists(storageFile))
                    File.Delete(storageFile);
                using var repo = new LiteRepository(storageFile);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    repo.Insert(faker());
                }
                
                sw.Restart();
                for (int i = 0; i < count; i++)
                {
                    repo.Insert(faker());
                }

                sw.Stop();
                Console.WriteLine($"{name} {typeof(T).Name}: {sw.ElapsedMilliseconds}, {count * 1000 / sw.ElapsedMilliseconds} OPS");
            }
        }
        
        static void Track(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("TrackOfCheckpoints benchmark. Supply number of checkpoints in one cycle and number of cycles");
                Console.WriteLine("Example: benchmark.exe track 1000 100");
                return;
            }
            var cps = int.Parse(args[0]);
            var cycles = int.Parse(args[1]);
            Console.WriteLine($"TrackOfCheckpoints benchmark. Append {cps} checkpoints {cycles} iterations");
            var incrementalWithCustomSortRunner = new InstanceRunner(() => new TrackOfCheckpoints(new DateTime(1), FinishCriteria.FromForcedFinish()));
            var cyclicRunner = new InstanceRunner(() => new TrackOfCheckpointsCyclic(new DateTime(1), FinishCriteria.FromForcedFinish()));
            var runners = new[] {("Incremental custom sort", incrementalWithCustomSortRunner), ("cyclic", cyclicRunner)};
            long baseLine = 0;
            foreach (var runner in runners)
            {
                runner.Item2.Work(100);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Start test: {runner.Item1}");
                Console.ForegroundColor = ConsoleColor.Gray;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < cycles; i++)
                {
                    runner.Item2.Work(cps);
                    if (i % 10 == 0)
                        Console.Write($"{i * 100 / cycles}% ");
                }
                sw.Stop();
                Console.WriteLine("100%");
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                if (baseLine == 0)
                    baseLine = sw.ElapsedMilliseconds;
                Console.WriteLine($"{runner.Item1}: Total={sw.ElapsedMilliseconds}ms, PerCycle={sw.ElapsedMilliseconds/(double)cycles:F2}ms, Relative={sw.ElapsedMilliseconds*100/baseLine}%");
            }
        }
    }
}