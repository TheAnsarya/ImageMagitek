﻿using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using ImageMagitek.Codec;
using ImageMagitek.Colors;

namespace ImageMagitek.Benchmarks;

public class Snes3bppDecodeToImage {
	private const string _nativeFileName = "Snes3bppDecodeToImageNative.bin";
	private const string _genericFileName = "Snes3bppDecodeToImageGeneric.bin";
	private const string _genericCodecFileName = @"Resources\SNES3bpp.xml";
	private const string _paletteFileName = @"_palettes\DefaultRgba32.json";
	private const string _outputDirectory = @"D:\ImageMagitekTest\Benchmark\";
	private const string _codecSchemaFileName = @"_schemas\CodecSchema.xsd";

	private IGraphicsCodec _codec;

	private DataFile _df;
	private Palette _pal;
	private ScatteredArranger _arranger;

	[GlobalSetup(Target = nameof(DecodeNative))]
	public void GlobalSetupNative() {
		var palContents = File.ReadAllText(_paletteFileName);
		_pal = PaletteJsonSerializer.ReadPalette(palContents, new ColorFactory());

		_codec = new Snes3bppCodec(8, 8);
		Setup(_nativeFileName, "native");
	}

	[GlobalSetup(Target = nameof(DecodeGeneric))]
	public void GlobalSetupGeneric() {
		var palContents = File.ReadAllText(_paletteFileName);
		_pal = PaletteJsonSerializer.ReadPalette(palContents, new ColorFactory());

		//var codecFileName = Path.Combine(Directory.GetCurrentDirectory(), "Resources", _genericCodecFileName);
		var serializer = new XmlGraphicsFormatReader(_codecSchemaFileName);
		var format = serializer.LoadFromFile(_genericCodecFileName);
		_codec = new IndexedFlowGraphicsCodec((FlowGraphicsFormat)format.AsSuccess.Result);

		Setup(_genericFileName, "generic");
	}

	public void Setup(string dataFileName, string arrangerName) {
		using (var fs = File.Create(dataFileName)) {
			var rng = new Random();
			var data = new byte[3 * 16 * 32];
			rng.NextBytes(data);
			fs.Write(data);
		}

		_df = new DataFile("df", Path.GetFullPath(dataFileName));

		_arranger = new ScatteredArranger(arrangerName, PixelColorType.Indexed, ElementLayout.Tiled, 16, 32, 8, 8);

		for (var y = 0; y < _arranger.ArrangerElementSize.Height; y++) {
			for (var x = 0; x < _arranger.ArrangerElementSize.Width; x++) {
				var el = new ArrangerElement(x * 8, y * 8, _df, (24 * x) + (24 * x * y), _codec, _pal);
				_arranger.SetElement(el, x, y);
			}
		}
	}

	[GlobalCleanup(Target = nameof(DecodeNative))]
	public void GlobalCleanupNative() {
		_df.Close();
		File.Delete(_nativeFileName);
	}

	[GlobalCleanup(Target = nameof(DecodeGeneric))]
	public void GlobalCleanupGeneric() {
		_df.Close();
		File.Delete(_genericFileName);
	}

	[Benchmark(Baseline = true)]
	public void DecodeNative() {
		for (var i = 0; i < 100; i++) {
			var outputFileName = Path.Combine(_outputDirectory, $"Native.{i}.bmp");

			var image = new IndexedImage(_arranger);
			image.ExportImage(outputFileName, new ImageSharpFileAdapter());
		}
	}

	[Benchmark]
	public void DecodeGeneric() {
		for (var i = 0; i < 100; i++) {
			var outputFileName = Path.Combine(_outputDirectory, $"Generic.{i}.bmp");

			var image = new IndexedImage(_arranger);
			image.ExportImage(outputFileName, new ImageSharpFileAdapter());
		}
	}
}
