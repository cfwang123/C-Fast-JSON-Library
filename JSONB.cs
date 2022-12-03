using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Q {
	public sealed class JSONB {
		static Encoding GBK = Encoding.GetEncoding("GBK");
		public enum StringEncoding { Unicode = 0, GBK, UTF8, ASCII }
		public enum NType {
			Predefines = 0, //value: 0=null,1=false,2=true,NaN,Inf,-Inf
			Array = 1,
			Object = 2,
			Int = 3,
			Long = 4,
			Float = 5,
			Double = 6,
			Decimal = 7,
			DateTime = 8,
			String = 9,
			Bytes = 10,
			EndList = 11, //Array或Object结束标记
			Unknown = 12,
		}
		const int P_NULL = 0, P_FALSE = 1, P_TRUE = 2, P_NAN = 3, P_INF = 4, P_NEGINF = 5, P_DBL_NAN = 6, P_DBL_INF = 7, P_DBL_NEGINF = 8
			//数字值格式
			, FMT_DEFAULT = 0
			, FMT_U8 = 1
			, FMT_MINUS_U8 = 2
			, FMT_U16 = 3
			, FMT_MINUS_U16 = 4
			, FMT_U24 = 5
			, FMT_MINUS_U24 = 6
			, FMT_U32 = 7
			, FMT_MINUS_U32 = 8
			, FMT_MINUS_ONE = 9
			, FMT_ZERO = 10
			, FMT_ONE = 11
			, FMT_TWO = 12
			, FMT_THREE = 13
			, FMT_FOUR = 14
			, FMT_FIVE = 15
			//String格式
			, STR_DEFAULT = 0
			, STR_EMPTY = 1
			, STR_MINUS_ONE = 2
			, STR_ZERO = 3
			, STR_ONE = 4
			, STR_TWO = 5
			, STR_THREE = 6
			, STR_ON = 7
			, STR_OFF = 8
			, STR_YES = 9
			, STR_NO = 10
		;
		static readonly Dictionary<string, int> PRESTRMAP = new Dictionary<string, int>() {
			{ "", STR_EMPTY },
			{ "-1", STR_MINUS_ONE },
			{ "0", STR_ZERO },
			{ "1", STR_ONE },
			{ "2", STR_TWO },
			{ "3", STR_THREE },
			{ "on", STR_ON },
			{ "off", STR_OFF },
			{ "yes", STR_YES },
			{ "no", STR_NO },
		};
		static readonly string[] PRESTRINGS = new string[] { "", "-1", "0", "1", "2", "3", "on", "off", "yes", "no" };

		Stream s;
		StringEncoding enc;
		Encoding encoding;

		public JSONB(Stream s, StringEncoding enc = StringEncoding.GBK) {
			this.s = s;
			this.enc = enc;
			encoding = GetEncoding(enc);
		}

		public static Encoding GetEncoding(StringEncoding enc) {
			switch(enc) {
				case StringEncoding.GBK:
				default:
					return GBK;
				case StringEncoding.UTF8:
					return Encoding.UTF8;
				case StringEncoding.Unicode:
					return Encoding.Unicode;
				case StringEncoding.ASCII:
					return Encoding.ASCII;
			}
		}

		public static byte[] GenBytes(JSON js, StringEncoding enc = StringEncoding.GBK, int initiallen = 2000) {
			using(var ms = new MemoryStream(initiallen)) {
				var J = new JSONB(ms, enc);
				J.PutJSON(js);
				return ms.ToArray();
			}
		}

		public void WriteTypeByte(NType type, int value) => s.WriteByte((byte)((int)type | (value << 4)));

		public void PutArrayStart() => WriteTypeByte(NType.Array, 0);
		public void PutArrayEnd() => WriteTypeByte(NType.EndList, 0);
		public void PutObjectStart() => WriteTypeByte(NType.Object, 0);
		public void PutObjectEnd() => WriteTypeByte(NType.EndList, 0);

		public void PutJSON(JSON js) {
			if(js.type == JSON.NodeType.Expression)
				js.ParseExpression();
			switch(js.type) {
				case JSON.NodeType.Null:
					WriteTypeByte(NType.Predefines, P_NULL);
					break;
				case JSON.NodeType.Bool:
					WriteTypeByte(NType.Predefines, js.AsBool ? P_TRUE : P_FALSE);
					break;
				case JSON.NodeType.Int:
					if(!PutValueInt(NType.Int, js.AsInt)) {
						WriteTypeByte(NType.Int, FMT_DEFAULT);
						int ival = js.AsInt;
						s.WriteByte((byte)ival);
						s.WriteByte((byte)(ival >> 8));
						s.WriteByte((byte)(ival >> 16));
						s.WriteByte((byte)(ival >> 24));
					}
					break;
				case JSON.NodeType.Long:
					if(!PutValueLong(NType.Long, js.AsLong)) {
						WriteTypeByte(NType.Long, FMT_DEFAULT);
						long lval = js.AsLong;
						s.WriteByte((byte)lval);
						s.WriteByte((byte)(lval >> 8));
						s.WriteByte((byte)(lval >> 16));
						s.WriteByte((byte)(lval >> 24));
						s.WriteByte((byte)(lval >> 32));
						s.WriteByte((byte)(lval >> 40));
						s.WriteByte((byte)(lval >> 48));
						s.WriteByte((byte)(lval >> 56));
					}
					break;
				case JSON.NodeType.Float: {
					float fval = js.AsFloat;
					int ifval = (int)fval;
					if(fval - ifval == 0) {
						if(PutValueInt(NType.Float, ifval))
							return;
					}
					if(float.IsNaN(fval))
						WriteTypeByte(NType.Predefines, P_NAN);
					else if(float.IsPositiveInfinity(fval))
						WriteTypeByte(NType.Predefines, P_INF);
					else if(float.IsNegativeInfinity(fval))
						WriteTypeByte(NType.Predefines, P_NEGINF);
					else {
						WriteTypeByte(NType.Float, FMT_DEFAULT);
						s.Write(BitConverter.GetBytes(fval), 0, 4);
					}
				}
				break;
				case JSON.NodeType.Double: {
					double dval = js.AsDouble;
					long ldval = (long)dval;
					if(dval - ldval == 0) {
						if(PutValueLong(NType.Double, ldval))
							return;
					}
					if(double.IsNaN(dval))
						WriteTypeByte(NType.Predefines, P_DBL_NAN);
					else if(double.IsPositiveInfinity(dval))
						WriteTypeByte(NType.Predefines, P_DBL_INF);
					else if(double.IsNegativeInfinity(dval))
						WriteTypeByte(NType.Predefines, P_DBL_NEGINF);
					else {
						WriteTypeByte(NType.Double, FMT_DEFAULT);
						s.Write(BitConverter.GetBytes(dval), 0, 8);
					}
				}
				break;
				case JSON.NodeType.Decimal: {
					decimal decval = js.AsDecimal;
					long lval = (long)decval;
					if(decval - lval == 0) {
						if(PutValueLong(NType.Decimal, lval))
							return;
					}
					WriteTypeByte(NType.Decimal, FMT_DEFAULT);
					PutAscii(decval.ToString());
				}
				break;
				case JSON.NodeType.String: {
					string sval = js.Value;
					if(PRESTRMAP.TryGetValue(sval, out int preval))
						WriteTypeByte(NType.String, preval);
					else {
						WriteTypeByte(NType.String, STR_DEFAULT);
						PutString(js.Value);
					}
				}
				break;
				case JSON.NodeType.ByteArray: {
					byte[] b = (byte[])js.oval;
					if(b.Length == 0) WriteTypeByte(NType.Bytes, 1);
					else {
						WriteTypeByte(NType.Bytes, 0);
						PutLenField(b.Length);
						s.Write(b, 0, b.Length);
					}
				}
				break;
				case JSON.NodeType.DateTime:
					WriteTypeByte(NType.DateTime, FMT_DEFAULT);
					long lval2 = js.val.lval;
					s.WriteByte((byte)lval2);
					s.WriteByte((byte)(lval2 >> 8));
					s.WriteByte((byte)(lval2 >> 16));
					s.WriteByte((byte)(lval2 >> 24));
					s.WriteByte((byte)(lval2 >> 32));
					s.WriteByte((byte)(lval2 >> 40));
					s.WriteByte((byte)(lval2 >> 48));
					s.WriteByte((byte)(lval2 >> 56));
					break;
				case JSON.NodeType.Array:
					if(js.Count == 0)
						WriteTypeByte(NType.Array, 1);
					else {
						WriteTypeByte(NType.Array, 0);
						foreach(var v in js)
							PutJSON(v);
						WriteTypeByte(NType.EndList, 0);
					}
					break;
				case JSON.NodeType.Object:
					if(js.Count == 0)
						WriteTypeByte(NType.Object, 1);
					else {
						WriteTypeByte(NType.Object, 0);
						foreach(var (k, v) in js.KeyVals) {
							PutJSON(v);
							PutString(k);
						}
						WriteTypeByte(NType.EndList, 0);
					}
					break;
			}
		}

		bool PutValueLong(NType typ, long lval) {
			if(lval >= -1 && lval <= 5)
				WriteTypeByte(typ, FMT_MINUS_ONE + (int)lval + 1);
			else if(lval >= 0) {
				if(lval < 256) {
					WriteTypeByte(typ, FMT_U8);
					s.WriteByte((byte)lval);
				}
				else if(lval < 256 * 256) {
					WriteTypeByte(typ, FMT_U16);
					s.WriteByte((byte)lval);
					s.WriteByte((byte)(lval >> 8));
				}
				else if(lval < 256 * 256 * 256) {
					WriteTypeByte(typ, FMT_U24);
					s.WriteByte((byte)lval);
					s.WriteByte((byte)(lval >> 8));
					s.WriteByte((byte)(lval >> 16));
				}
				else if(lval < 256 * 256 * 256 * 256L) {
					WriteTypeByte(typ, FMT_U32);
					s.WriteByte((byte)lval);
					s.WriteByte((byte)(lval >> 8));
					s.WriteByte((byte)(lval >> 16));
					s.WriteByte((byte)(lval >> 24));
				}
				else return false;
			}
			else {
				if(lval > -256) {
					WriteTypeByte(typ, FMT_MINUS_U8);
					s.WriteByte((byte)-lval);
				}
				else if(lval > -256 * 256) {
					lval = -lval;
					WriteTypeByte(typ, FMT_MINUS_U16);
					s.WriteByte((byte)lval);
					s.WriteByte((byte)(lval >> 8));
				}
				else if(lval > -256 * 256 * 256) {
					lval = -lval;
					WriteTypeByte(typ, FMT_MINUS_U24);
					s.WriteByte((byte)lval);
					s.WriteByte((byte)(lval >> 8));
					s.WriteByte((byte)(lval >> 16));
				}
				else if(lval > -256 * 256 * 256 * 256L) {
					lval = -lval;
					WriteTypeByte(typ, FMT_MINUS_U32);
					s.WriteByte((byte)lval);
					s.WriteByte((byte)(lval >> 8));
					s.WriteByte((byte)(lval >> 16));
					s.WriteByte((byte)(lval >> 24));
				}
				else return false;
			}
			return true;
		}

		bool PutValueInt(NType typ, int ival) {
			if(ival >= -1 && ival <= 5)
				WriteTypeByte(typ, FMT_MINUS_ONE + ival + 1);
			else if(ival >= 0) {
				if(ival < 256) {
					WriteTypeByte(typ, FMT_U8);
					s.WriteByte((byte)ival);
				}
				else if(ival < 256 * 256) {
					WriteTypeByte(typ, FMT_U16);
					s.WriteByte((byte)ival);
					s.WriteByte((byte)(ival >> 8));
				}
				else if(ival < 256 * 256 * 256) {
					WriteTypeByte(typ, FMT_U24);
					s.WriteByte((byte)ival);
					s.WriteByte((byte)(ival >> 8));
					s.WriteByte((byte)(ival >> 16));
				}
				else return false;
			}
			else {
				if(ival > -256) {
					WriteTypeByte(typ, FMT_MINUS_U8);
					s.WriteByte((byte)-ival);
				}
				else if(ival > -256 * 256) {
					ival = -ival;
					WriteTypeByte(typ, FMT_MINUS_U16);
					s.WriteByte((byte)ival);
					s.WriteByte((byte)(ival >> 8));
				}
				else if(ival > -256 * 256 * 256) {
					ival = -ival;
					WriteTypeByte(typ, FMT_MINUS_U24);
					s.WriteByte((byte)ival);
					s.WriteByte((byte)(ival >> 8));
					s.WriteByte((byte)(ival >> 16));
				}
				else return false;
			}
			return true;
		}

		public void PutString(string v) {
			byte[] b = encoding.GetBytes(v);
			PutLenField(b.Length);
			s.Write(b, 0, b.Length);
		}

		public void PutAscii(string v) {
			byte[] b = Encoding.ASCII.GetBytes(v);
			PutLenField(b.Length);
			s.Write(b, 0, b.Length);
		}

		public void PutLenField(int len) {
			if(len < 128) {
				s.WriteByte((byte)len);
			}
			else if(len < 128 * 128) {
				s.WriteByte((byte)(len | 0x80));
				s.WriteByte((byte)((len >> 7) & 127));
			}
			else if(len < 128 * 128 * 128) {
				s.WriteByte((byte)(len | 0x80));
				s.WriteByte((byte)((len >> 7) | 0x80));
				s.WriteByte((byte)((len >> 14) & 127));
			}
			else {
				s.WriteByte((byte)(len | 0x80));
				s.WriteByte((byte)((len >> 7) | 0x80));
				s.WriteByte((byte)((len >> 14) | 0x80));
				s.WriteByte((byte)((len >> 21) & 255));
			}
		}

		public static int GetLenFieldLength(int len) {
			if(len < 128) return 1;
			else if(len < 128 * 128) return 2;
			else if(len < 128 * 128 * 128) return 3;
			else return 4;
		}

		static (int lenlen, int len) ReadLenField(byte[] b, int pos) {
			if(b[pos] < 128) return (1, b[pos]);
			else if(b[pos + 1] < 128) return (2, (b[pos] & 0x7f) | ((b[pos + 1] & 0x7f) << 7));
			else if(b[pos + 2] < 128) return (3, (b[pos] & 0x7f) | ((b[pos + 1] & 0x7f) << 7) | ((b[pos + 2] & 0x7f) << 14));
			else return (4, (b[pos] & 0x7f) | ((b[pos + 1] & 0x7f) << 7) | ((b[pos + 2] & 0x7f) << 14) | (b[pos + 3] << 21));
		}

		static (int len, string str) ReadString(byte[] b, int pos, Encoding enc) {
			var (lenlen, len) = ReadLenField(b, pos);
			return (lenlen + len, enc.GetString(b, pos + lenlen, len));
		}

		static (int len, byte[] b) ReadBytes(byte[] b, int pos) {
			var (lenlen, len) = ReadLenField(b, pos);
			var val = new byte[len];
			Array.Copy(b, pos + lenlen, val, 0, len);
			return (lenlen + len, val);
		}

		static (int len, long val) ReadIntFormat(int format, byte[] b, int pos) {
			switch(format) {
				case FMT_MINUS_ONE:
				case FMT_ZERO:
				case FMT_ONE:
				case FMT_TWO:
				case FMT_THREE:
				case FMT_FOUR:
				case FMT_FIVE:
					return (0, format - FMT_ZERO);
				case FMT_U8:
					return (1, b[pos]);
				case FMT_MINUS_U8:
					return (1, -b[pos]);
				case FMT_U16:
					return (2, b[pos] | (b[pos + 1] << 8));
				case FMT_MINUS_U16:
					return (2, -(b[pos] | (b[pos + 1] << 8)));
				case FMT_U24:
					return (3, b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16));
				case FMT_MINUS_U24:
					return (3, -(b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16)));
				case FMT_U32:
					return (4, (uint)((b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16) | (b[pos + 3] << 24))));
				case FMT_MINUS_U32:
					return (4, -(uint)(b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16) | (b[pos + 3] << 24)));
				default:
					return (100000, 0);
			}
		}

		public static JSON Parse(byte[] b, int pos = 0, int len = -1, StringEncoding enc = StringEncoding.GBK) {
			if(len < 0) len = b.Length - pos;
			if(b.Length < pos + len) return default;
			try {
				return ParseJSONB(b, pos, len, GetEncoding(enc)).js;
			}
			catch { return default; }
		}

		public static (int usedbytes, JSON js) ParseJSONB(byte[] b, int pos, int len, Encoding enc) {
			JSON js = default;
			int typ = b[pos], format, i, vlen, oldpos = pos;
			long lval;
			i = typ & 0xf;
			format = typ >> 4;
			NType ntype = Enum.IsDefined(typeof(NType), i) ? (NType)i : NType.Unknown;
			pos++;
			switch(ntype) {
				case NType.String:
					if(format >= 1 && format - 1 < PRESTRINGS.Length)
						return (1, PRESTRINGS[format - 1]);
					(vlen, js) = ReadString(b, pos, enc);
					pos += vlen;
					break;
				case NType.Bytes:
					if(format == 1)
						return (1, new byte[0]);
					(vlen, js) = ReadBytes(b, pos);
					pos += vlen;
					break;
				case NType.DateTime:
					js.val.lval = (long)b[pos] | ((long)b[pos + 1] << 8) | ((long)b[pos + 2] << 16) | ((long)b[pos + 3] << 24) | ((long)b[pos + 4] << 32) | ((long)b[pos + 5] << 40) | ((long)b[pos + 6] << 48) | ((long)b[pos + 7] << 56);
					js.type = JSON.NodeType.DateTime;
					pos += 8;
					break;
				case NType.Int:
					if(format == FMT_DEFAULT) {
						js = b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16) | (b[pos + 3] << 24);
						pos += 4;
					}
					else {
						(vlen, lval) = ReadIntFormat(format, b, pos);
						js = (int)lval;
						pos += vlen;
					}
					break;
				case NType.Long:
					if(format == FMT_DEFAULT) {
						js = (long)b[pos] | ((long)b[pos + 1] << 8) | ((long)b[pos + 2] << 16) | ((long)b[pos + 3] << 24) | ((long)b[pos + 4] << 32) | ((long)b[pos + 5] << 40) | ((long)b[pos + 6] << 48) | ((long)b[pos + 7] << 56);
						pos += 8;
					}
					else {
						(vlen, js) = ReadIntFormat(format, b, pos);
						pos += vlen;
					}
					break;
				case NType.Float:
					if(format == FMT_DEFAULT) {
						js = BitConverter.ToSingle(b, pos);
						pos += 4;
					}
					else {
						(vlen, lval) = ReadIntFormat(format, b, pos);
						js = (float)lval;
						pos += vlen;
					}
					break;
				case NType.Double:
					if(format == FMT_DEFAULT) {
						js = BitConverter.ToDouble(b, pos);
						pos += 8;
					}
					else {
						(vlen, lval) = ReadIntFormat(format, b, pos);
						js = (double)lval;
						pos += vlen;
					}
					break;
				case NType.Decimal:
					if(format == FMT_DEFAULT) {
						string strval;
						(vlen, strval) = ReadString(b, pos, enc);
						pos += vlen;
						if(decimal.TryParse(strval, out var d))
							js = d;
						else js = 0M;
					}
					else {
						(vlen, lval) = ReadIntFormat(format, b, pos);
						js = (decimal)lval;
						pos += vlen;
					}
					break;
				case NType.Array:
					js = JSON.newArray();
					if(format == 1) return (1, js);
					while(true) {
						if(b[pos] == (byte)NType.EndList) {
							pos++;
							break;
						}
						JSON vjs;
						(vlen, vjs) = ParseJSONB(b, pos, len - (pos - oldpos), enc);
						if(vlen == 0) return default;
						pos += vlen;
						js.Add(ref vjs);
					}
					return (pos - oldpos, js);
				case NType.Object:
					js = JSON.newObject();
					if(format == 1) return (1, js);
					while(true) {
						if(b[pos] == (byte)NType.EndList) {
							pos++;
							break;
						}
						JSON vjs;
						(vlen, vjs) = ParseJSONB(b, pos, len - (pos - oldpos), enc);
						if(vlen == 0) return default;
						pos += vlen;
						string vkey;
						(vlen, vkey) = ReadString(b, pos, enc);
						pos += vlen;
						js.Add(vkey, ref vjs);
					}
					return (pos - oldpos, js);
				case NType.Predefines:
					switch(format) {
						case P_NULL: default: return (1, (string)null);
						case P_FALSE: return (1, false);
						case P_TRUE: return (1, true);
						case P_NAN: return (1, float.NaN);
						case P_INF: return (1, float.PositiveInfinity);
						case P_NEGINF: return (1, float.NegativeInfinity);
						case P_DBL_NAN: return (1, double.NaN);
						case P_DBL_INF: return (1, double.PositiveInfinity);
						case P_DBL_NEGINF: return (1, double.NegativeInfinity);
					}
			}
			return (pos - oldpos, js);
		}
	}
}
