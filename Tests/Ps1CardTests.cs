using System.IO;
using System.Linq;
using MemcardRex;
using MemcardRex.Enums;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class Ps1CardTests
	{
		[Test]
		public void GmeHeaderTest()
		{
			var card = new Ps1Card();
			card.Format();
			using (var stream = new MemoryStream())
				card.SaveTo(stream, MemoryCardType.Gme);

			var expected = new byte[] {0x31, 0x32, 0x33, 0x2D, 0x34, 0x35, 0x36, 0x2D, 0x53, 0x54, 0x44}; // 123-456-STD
			Assert.That(card.GmeHeader.Take(expected.Length), Is.EqualTo(expected));
		}
	}
}
