﻿using BenchmarkDotNet.Running;

namespace ImageMagitek.Benchmarks;

internal class Program {
	private static void Main(string[] args) =>
		//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
		//BenchmarkRunner.Run(typeof(FileStreamReopenPerRead));
		//BenchmarkRunner.Run(typeof(Snes3bppDecodeToImage));
		BenchmarkRunner.Run(typeof(ColorRgbaToBgra));
}
