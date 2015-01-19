//Shift-JIS converter class
//Shendo 2009-2012

using System.Text;

namespace MemcardRex.Utils
{
	internal static class CharConverter
	{
		//Convert SJIS characters to ASCII equivalent
		public static string SjisToAscii(byte[] data)
		{
			StringBuilder output = new StringBuilder();
			for (var idx = 0; idx < data.Length; idx += 2)
				switch ((data[idx] << 8) | data[idx + 1])
				{
					case 0x0000: //End of the string
						return output.ToString();

					case 0x8140:
						output.Append("  ");
						break;

					case 0x8143:
						output.Append(',');
						break;

					case 0x8144:
						output.Append('.');
						break;

					case 0x8145:
						output.Append('·');
						break;

					case 0x8146:
						output.Append(':');
						break;

					case 0x8147:
						output.Append(';');
						break;

					case 0x8148:
						output.Append('?');
						break;

					case 0x8149:
						output.Append('!');
						break;

					case 0x814F:
						output.Append('^');
						break;

					case 0x8151:
						output.Append('_');
						break;

					case 0x815B:
					case 0x815C:
					case 0x815D:
						output.Append('-');
						break;

					case 0x815E:
						output.Append('/');
						break;

					case 0x815F:
						output.Append('\\');
						break;

					case 0x8160:
						output.Append('~');
						break;

					case 0x8161:
						output.Append('|');
						break;

					case 0x8168:
						output.Append('"');
						break;

					case 0x8169:
						output.Append('(');
						break;

					case 0x816A:
						output.Append(')');
						break;

					case 0x816D:
						output.Append('[');
						break;

					case 0x816E:
						output.Append(']');
						break;

					case 0x816F:
						output.Append('{');
						break;

					case 0x8170:
						output.Append('}');
						break;

					case 0x817B:
						output.Append('+');
						break;

					case 0x817C:
						output.Append('-');
						break;

					case 0x817D:
						output.Append('±');
						break;

					case 0x817E:
						output.Append('*');
						break;

					case 0x8180:
						output.Append('÷');
						break;

					case 0x8181:
						output.Append('=');
						break;

					case 0x8183:
						output.Append('<');
						break;

					case 0x8184:
						output.Append('>');
						break;

					case 0x818A:
						output.Append('°');
						break;

					case 0x818B:
						output.Append('\'');
						break;

					case 0x818C:
						output.Append('\"');
						break;

					case 0x8190:
						output.Append('$');
						break;

					case 0x8193:
						output.Append('%');
						break;

					case 0x8194:
						output.Append('#');
						break;

					case 0x8195:
						output.Append('&');
						break;

					case 0x8196:
						output.Append('*');
						break;

					case 0x8197:
						output.Append('@');
						break;

					case 0x824F:
						output.Append('0');
						break;

					case 0x8250:
						output.Append('1');
						break;

					case 0x8251:
						output.Append('2');
						break;

					case 0x8252:
						output.Append('3');
						break;

					case 0x8253:
						output.Append('4');
						break;

					case 0x8254:
						output.Append('5');
						break;

					case 0x8255:
						output.Append('6');
						break;

					case 0x8256:
						output.Append('7');
						break;

					case 0x8257:
						output.Append('8');
						break;

					case 0x8258:
						output.Append('9');
						break;

					case 0x8260:
						output.Append('A');
						break;

					case 0x8261:
						output.Append('B');
						break;

					case 0x8262:
						output.Append('C');
						break;

					case 0x8263:
						output.Append('D');
						break;

					case 0x8264:
						output.Append('E');
						break;

					case 0x8265:
						output.Append('F');
						break;

					case 0x8266:
						output.Append('G');
						break;

					case 0x8267:
						output.Append('H');
						break;

					case 0x8268:
						output.Append('I');
						break;

					case 0x8269:
						output.Append('J');
						break;

					case 0x826A:
						output.Append('K');
						break;

					case 0x826B:
						output.Append('L');
						break;

					case 0x826C:
						output.Append('M');
						break;

					case 0x826D:
						output.Append('N');
						break;

					case 0x826E:
						output.Append('O');
						break;

					case 0x826F:
						output.Append('P');
						break;

					case 0x8270:
						output.Append('Q');
						break;

					case 0x8271:
						output.Append('R');
						break;

					case 0x8272:
						output.Append('S');
						break;

					case 0x8273:
						output.Append('T');
						break;

					case 0x8274:
						output.Append('U');
						break;

					case 0x8275:
						output.Append('V');
						break;

					case 0x8276:
						output.Append('W');
						break;

					case 0x8277:
						output.Append('X');
						break;

					case 0x8278:
						output.Append('Y');
						break;

					case 0x8279:
						output.Append('Z');
						break;

					case 0x8281:
						output.Append('a');
						break;

					case 0x8282:
						output.Append('b');
						break;

					case 0x8283:
						output.Append('c');
						break;

					case 0x8284:
						output.Append('d');
						break;

					case 0x8285:
						output.Append('e');
						break;

					case 0x8286:
						output.Append('f');
						break;

					case 0x8287:
						output.Append('g');
						break;

					case 0x8288:
						output.Append('h');
						break;

					case 0x8289:
						output.Append('i');
						break;

					case 0x828A:
						output.Append('j');
						break;

					case 0x828B:
						output.Append('k');
						break;

					case 0x828C:
						output.Append('l');
						break;

					case 0x828D:
						output.Append('m');
						break;

					case 0x828E:
						output.Append('n');
						break;

					case 0x828F:
						output.Append('o');
						break;

					case 0x8290:
						output.Append('p');
						break;

					case 0x8291:
						output.Append('q');
						break;

					case 0x8292:
						output.Append('r');
						break;

					case 0x8293:
						output.Append('s');
						break;

					case 0x8294:
						output.Append('t');
						break;

					case 0x8295:
						output.Append('u');
						break;

					case 0x8296:
						output.Append('v');
						break;

					case 0x8297:
						output.Append('w');
						break;

					case 0x8298:
						output.Append('x');
						break;

					case 0x8299:
						output.Append('y');
						break;

					case 0x829A:
						output.Append('z');
						break;
				}
			return output.ToString();
		}
	}
}