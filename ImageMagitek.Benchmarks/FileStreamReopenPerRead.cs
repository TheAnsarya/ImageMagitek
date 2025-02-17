﻿using System;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace ImageMagitek.Benchmarks;

/// <summary>
/// Benchmark to determine performace impact of continually reopening FileStream objects contained by DataFiles
/// </summary>
public class FileStreamReopenPerRead {
	[Params(16, 64, 256, 512, 16384)]
	public int TotalReadSize;

	private const int SizePerRead = 16;
	private const string FileName = "FileStreamReopenPerReadTestData.bin";

	[GlobalSetup]
	public void GlobalSetup() {
		using var fs = File.Create(FileName);

		var rng = new Random();
		var data = new byte[16384];
		rng.NextBytes(data);
		fs.Write(data);
	}

	[GlobalCleanup]
	public void GlobalCleanup() => File.Delete(FileName);

	[Benchmark]
	public void KeepOpen() {
		using var fs = File.OpenRead(FileName);
		using var br = new BinaryReader(fs);

		for (var i = 0; i < TotalReadSize; i += SizePerRead) {
			_ = fs.Seek(i, SeekOrigin.Begin);
			_ = br.ReadBytes(SizePerRead);
		}
	}

	[Benchmark]
	public void ReopenPerRead() {
		for (var i = 0; i < TotalReadSize; i += SizePerRead) {
			using var fs = File.OpenRead(FileName);
			using var br = new BinaryReader(fs);

			_ = fs.Seek(i, SeekOrigin.Begin);
			_ = br.ReadBytes(SizePerRead);
		}
	}
}
