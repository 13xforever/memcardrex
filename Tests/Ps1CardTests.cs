using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemcardRex;
using MemcardRex.Enums;
using MemcardRex.Enums.MemoryCard;
using MemcardRex.Utils;
using NUnit.Framework;
using Type = MemcardRex.Enums.MemoryCard.Type;

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
				card.ExportTo(stream, Type.Gme);

			var expected = new byte[] {0x31, 0x32, 0x33, 0x2D, 0x34, 0x35, 0x36, 0x2D, 0x53, 0x54, 0x44}; // 123-456-STD
			Assert.That(card.GmeHeader.Take(expected.Length), Is.EqualTo(expected));
		}

		[Test]
		public void VarianceTest()
		{
			Action<IList<int>> action = l => { Console.WriteLine(l.Count); };
			var array = new int[10];
			var list = new List<int>(10);
			action(array);
			action(list);
		}

		[Test]
		public void BitShiftTest()
		{
			const int numberOfBytes = 0x112233;
			var oldArray = new byte[4];
			oldArray[0] = (byte)(numberOfBytes & 0xFF);
			oldArray[1] = (byte)((numberOfBytes & 0xFF00) >> 8);
			oldArray[2] = (byte)((numberOfBytes & 0xFF0000) >> 16);

			var newArray = new byte[4];
			newArray[0] = (byte)(numberOfBytes & 0xFF);
			newArray[1] = (byte)((numberOfBytes >> 8) & 0xff);
			newArray[2] = (byte)((numberOfBytes >> 16) & 0xff);

			var bitConverter = BitConverter.GetBytes(numberOfBytes);
			Assert.That(newArray, Is.EqualTo(oldArray));
			Assert.That(bitConverter, Is.EqualTo(oldArray));
		}

		[Test]
		public void EnumTest()
		{
			ushort customRegion = 1;
			var invalidMemoryCardSaveRegion = (SaveRegion)customRegion;
			Assert.That((ushort)invalidMemoryCardSaveRegion, Is.EqualTo(customRegion));
		}

		[Test]
		public void ArrayTest()
		{
			var test = new byte[2, 2]
						{
							{1, 2},
							{3, 4},
						};
			test.Clear();
			Assert.That(test[0,0], Is.EqualTo(0));
			Assert.That(test[0,1], Is.EqualTo(0));
			Assert.That(test[1,0], Is.EqualTo(0));
			Assert.That(test[1,1], Is.EqualTo(0));
		}
	}
}
