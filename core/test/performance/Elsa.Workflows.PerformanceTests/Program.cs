using BenchmarkDotNet.Running;

BenchmarkSwitcher switcher = new(typeof(Program).Assembly);
switcher.Run(args);