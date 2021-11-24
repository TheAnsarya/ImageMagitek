using System.Collections.Generic;
using ImageMagitek.Services;
using ImageMagitek.UnitTests.TestFactories;
using NUnit.Framework;

namespace ImageMagitek.UnitTests;

public class ScatteredArrangerReversibilityTestCases {
	public static IEnumerable<TestCaseData> ReverseCases {
		get {
			var codecService = new CodecService(@"_schemas/CodecSchema.xsd");
			_ = codecService.LoadXmlCodecs(@"_codecs");
			var codecFactory = codecService.CodecFactory;

			var bubblesFontLocation = @"TestImages/2bpp/bubbles_font_2bpp.bmp";

			yield return new TestCaseData(
				ArrangerTestFactory.CreateIndexedArrangerFromImage(bubblesFontLocation,
					Colors.ColorModel.Bgr15,
					false,
					codecFactory,
					codecFactory.GetCodec("NES 2bpp", new System.Drawing.Size(8, 8))),
				bubblesFontLocation);

			yield return new TestCaseData(
				ArrangerTestFactory.CreateIndexedArrangerFromImage(bubblesFontLocation,
					Colors.ColorModel.Bgr15,
					false,
					codecFactory,
					codecFactory.GetCodec("SNES 2bpp", new System.Drawing.Size(8, 8))),
				bubblesFontLocation);
		}
	}
}
