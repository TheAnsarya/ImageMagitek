using System.Linq;
using ImageMagitek.ExtensionMethods;
using NUnit.Framework;

namespace ImageMagitek.UnitTests.ExtensionMethodTests;

public class TransposeArray2DTests {
	[TestCaseSource(typeof(TransposeArray2DTestCases), "TransposeCases")]
	public void TransposeArray2D_AsExpected(byte[,] source, byte[,] expected) {
		source.TransposeArray2D();
		CollectionAssert.AreEqual(source.Cast<byte>(), expected.Cast<byte>());
	}
}
