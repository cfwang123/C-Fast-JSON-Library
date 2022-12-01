using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace Q.JSON {

	public struct JSONF : IEnumerable<JSONF> {
		public static JSONF NOTEXIST = new JSONF(NodeType.NotExist, default, null);
		static DateTime EPOCH = new DateTime(1970, 1, 1);

		public enum NodeType { Null = 0, Array, Object, Int, Long, Float, Double, DateTime, Bool, Decimal, String, CustomObject, Expression, NotExist }
		public NodeType type;
		public DWord val; //i32 i64 float double datetime
		public object oval;
		public JSONF(NodeType type, DWord val, object oval) {
			this.type = type;
			this.val = val;
			this.oval = oval;
		}

		public ref JSONF this[int index] {
			get {
				if(type == NodeType.Array) {
					var arr = oval as RefList<JSONF>;
					if(index >= 0 && index < arr.Count)
						return ref arr[index];
				}
				return ref NOTEXIST;
			}
		}

		public ref JSONF this[string key] {
			get {
				if(type == NodeType.Object) {
					var arr = oval as RefDict<string,JSONF>;
					if(arr.posmap.TryGetValue(key, out int pos))
						return ref arr.items[pos];
					else {
						arr.Add(key, "");
						return ref arr[key];
					}
				}
				return ref NOTEXIST;
			}
		}

		public ref JSONF get(string key) {
			if(type == NodeType.Object) {
				var arr = oval as RefDict<string,JSONF>;
				if(arr.posmap.TryGetValue(key, out int pos))
					return ref arr.items[pos];
			}
			return ref NOTEXIST;
		}

		public int Count {
			get {
				if(type == NodeType.Array) return (oval as RefList<JSONF>).Count;
				else if(type == NodeType.Object) return (oval as RefDict<string, JSONF>).Count;
				else return 0;
			}
		}

		public void Add(string key, JSONF value) {
			if(type != NodeType.Object) return;
			var map = oval as RefDict<string, JSONF>;
			if(map.posmap.TryGetValue(key, out int pos))
				map.items[pos] = value;
			else map.Add(key, value);
		}

		public void Add(JSONF value) {
			if(type != NodeType.Array) return;
			var arr = oval as RefList<JSONF>;
			arr.Add(value);
		}

		public void Clear() {
			if(type == NodeType.Array) (oval as RefList<JSONF>).Clear();
			else if(type == NodeType.Object) (oval as RefDict<string, JSONF>).Clear();
		}

		public void EnsureCapacity(int capacity) {
			if(type == NodeType.Array) (oval as RefList<JSONF>).Capacity = capacity;
			else if(type == NodeType.Object) (oval as RefDict<string, JSONF>).Capacity = capacity;
		}

		public string Value {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return (string)oval;
					case NodeType.Int: return val.ival.ToString();
					case NodeType.Long: return val.lval.ToString();
					case NodeType.Float: return val.fval.ToString();
					case NodeType.Double: return val.dval.ToString();
					case NodeType.Bool: return val.boolval ? "1" : "0";
					case NodeType.DateTime: return val.timeval.ToString("yyyy-MM-dd HH:mm:ss");
					case NodeType.Decimal: return ((decimal)oval).ToString();
					case NodeType.Array: return "[Array]";
					case NodeType.Object: return "[Object]";
					case NodeType.CustomObject: return oval.ToString();
					default: return "";
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.String;
				oval = value;
			}
		}

		public int AsInt {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return int.TryParse((string)oval, out int v) ? v : 0;
					case NodeType.Int: return val.ival;
					case NodeType.Long: return val.ival;
					case NodeType.Float: return (int)val.fval;
					case NodeType.Double: return (int)val.dval;
					case NodeType.Bool: return val.boolval ? 1 : 0;
					case NodeType.Decimal: return (int)(decimal)oval;
					default: return 0;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Int;
				val.ival = value;
			}
		}

		public long AsLong {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return long.TryParse((string)oval, out long v) ? v : 0;
					case NodeType.Int: return val.ival;
					case NodeType.Long: return val.lval;
					case NodeType.Float: return (long)val.fval;
					case NodeType.Double: return (long)val.dval;
					case NodeType.Bool: return val.boolval ? 1 : 0;
					case NodeType.Decimal: return (long)(decimal)oval;
					default: return 0;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Long;
				val.lval = value;
			}
		}

		public float AsFloat {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return float.TryParse((string)oval, out float f) ? f : 0;
					case NodeType.Int: return val.ival;
					case NodeType.Long: return val.lval;
					case NodeType.Float: return val.fval;
					case NodeType.Double: return (float)val.dval;
					case NodeType.Bool: return val.boolval ? 1 : 0;
					case NodeType.Decimal: return (float)(decimal)oval;
					default: return 0;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Float;
				val.fval = value;
			}
		}

		public double AsDouble {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return double.TryParse((string)oval, out double d) ? d : 0;
					case NodeType.Int: return val.ival;
					case NodeType.Long: return val.lval;
					case NodeType.Float: return val.fval;
					case NodeType.Double: return val.dval;
					case NodeType.Bool: return val.boolval ? 1 : 0;
					case NodeType.Decimal: return (double)(decimal)oval;
					default: return 0;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Double;
				val.dval = value;
			}
		}

		public decimal AsDecimal {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return decimal.TryParse((string)oval, out var v) ? v : 0;
					case NodeType.Int: return val.ival;
					case NodeType.Long: return val.lval;
					case NodeType.Float: return (decimal)val.fval;
					case NodeType.Double: return (decimal)val.dval;
					case NodeType.Bool: return val.boolval ? 1 : 0;
					case NodeType.Decimal: return (decimal)oval;
					default: return 0;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Decimal;
				oval = value;
			}
		}

		public DateTime AsDatetime {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return DateTime.TryParse((string)oval, out var v) ? v : EPOCH;
					case NodeType.DateTime: return val.timeval;
					default: return EPOCH;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.DateTime;
				val.timeval = value;
			}
		}

		public bool AsBool {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return (string)oval != "" && (string)oval != "0";
					case NodeType.Int:
					case NodeType.Long: return val.lval != 0;
					case NodeType.Float: return val.fval != 0;
					case NodeType.Double: return val.dval != 0;
					case NodeType.Bool: return val.boolval;
					case NodeType.Decimal: return ((decimal)oval) != 0;
					case NodeType.Array: return (oval as RefList<JSONF>).Count > 0;
					case NodeType.Object: return (oval as RefDict<string,JSONF>).Count > 0;
					default: return false;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Bool;
				val.boolval = value;
			}
		}

		public static JSONF newArray(int capacity = 0) {
			return new JSONF(NodeType.Array, default, new RefList<JSONF>(capacity));
		}

		public static JSONF newObject(int capacity = 0, StringComparer scmp = null) {
			return new JSONF(NodeType.Object, default, new RefDict<string, JSONF>(capacity, scmp));
		}

		public static JSONF newArray(params JSONF[] items) {
			var list = new RefList<JSONF>();
			list.AddRange(items);
			return new JSONF(NodeType.Array, default, list);
		}

		public static JSONF newObject(params Pair<string, JSONF>[] items) {
			var map = new RefDict<string, JSONF>();
			foreach(var v in items)
				map.Add(v.key, v.value);
			return new JSONF(NodeType.Object, default, map);
		}
		public static JSONF NULL => new JSONF(NodeType.Null, default, null);
		public static JSONF newData(int v) => new JSONF(NodeType.Int, DWord.make(v), null);
		public static JSONF newData(long v) => new JSONF(NodeType.Long, DWord.make(v), null);
		public static JSONF newData(float v) => new JSONF(NodeType.Float, DWord.make(v), null);
		public static JSONF newData(double v) => new JSONF(NodeType.Double, DWord.make(v), null);
		public static JSONF newData(DateTime v) => new JSONF(NodeType.DateTime, DWord.make(v), null);
		public static JSONF newData(bool v) => new JSONF(NodeType.Bool, DWord.make(v ? 1 : 0), null);
		public static JSONF newData(decimal v) => new JSONF(NodeType.Decimal, default, v);
		public static JSONF newData(string v) => new JSONF(NodeType.String, default, v);
		public static JSONF newCustomData(object v) => new JSONF(NodeType.CustomObject, default, v);

		public static implicit operator JSONF(bool b) => JSONF.newData(b);
		public static implicit operator JSONF(int b) => JSONF.newData(b);
		public static implicit operator JSONF(long b) => JSONF.newData(b);
		public static implicit operator JSONF(float b) => JSONF.newData(b);
		public static implicit operator JSONF(double b) => JSONF.newData(b);
		public static implicit operator JSONF(DateTime b) => JSONF.newData(b);
		public static implicit operator JSONF(decimal b) => JSONF.newData(b);
		public static implicit operator JSONF(string b) => JSONF.newData(b);
		public static implicit operator string(JSONF b) => b.Value;

		public IEnumerator<JSONF> GetEnumerator() {
			if(type == NodeType.Array) {
				foreach(var v in (RefList<JSONF>)oval)
					yield return v;
			}
			else if(type == NodeType.Object) {
				foreach(var v in (RefDict<string,JSONF>)oval)
					yield return v.value;
			}
		}

		public IEnumerable<JSONF> Vals {
			get {
				if(type == NodeType.Array) {
					foreach(var v in (RefList<JSONF>)oval)
						yield return v;
				}
				else if(type == NodeType.Object) {
					foreach(var v in (RefDict<string,JSONF>)oval)
						yield return v.value;
				}
			}
		}

		public IEnumerable<string> Keys {
			get {
				if(type == NodeType.Object) {
					foreach(var v in (RefDict<string,JSONF>)oval)
						yield return v.key;
				}
			}
		}

		public IEnumerable<Pair<string,JSONF>> KeyVals {
			get {
				if(type == NodeType.Object) {
					foreach(var v in (RefDict<string, JSONF>)oval)
						yield return v;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			if(type == NodeType.Array) {
				foreach(var v in (RefList<JSONF>)oval)
					yield return v;
			}
			else if(type == NodeType.Object) {
				foreach(var v in (RefDict<string,JSONF>)oval)
					yield return v.value;
			}
		}

		[ThreadStatic] static StringBuilder _sb;
		public override string ToString() {
			_sb = _sb ?? new StringBuilder(2000);
			ToString(_sb);
			string s = _sb.ToString();
			_sb.Clear();
			return s;
		}

		public string ToString(int pre) {
			_sb = _sb ?? new StringBuilder(2000);
			ToString(_sb, pre);
			string s = _sb.ToString();
			_sb.Clear();
			return s;
		}

		public void ToString(StringBuilder sb, int pre = -1) {
			bool dl = false;
			int i, j, len;
			switch(type) {
				case NodeType.Array:
					var arr = oval as RefList<JSONF>;
					sb.Append('[');
					if(pre >= 0) {
						sb.Append("\r\n");
						for(i = 0;i <= pre;i++) sb.Append("  ");
					}
					for(i = 0, len = arr.Count;i < len;i++) {
						if(dl) {
							sb.Append(',');
							if(pre >= 0) {
								sb.Append("\r\n");
								for(j = 0;j <= pre;j++) sb.Append("  ");
							}
						}
						else dl = true;
						arr[i].ToString(sb, pre >= 0 ? (pre + 1) : -1);
					}
					if(pre >= 0) {
						sb.Append("\r\n");
						for(j = 1;j <= pre;j++) sb.Append("  ");
					}
					sb.Append(']');
					break;
				case NodeType.Object:
					var map = oval as RefDict<string,JSONF>;
					sb.Append('{');
					if(pre >= 0) {
						sb.Append("\r\n");
						for(i = 0;i <= pre;i++) sb.Append("  ");
					}
					foreach(string key in map.posmap.Keys) {
						if(dl) {
							sb.Append(',');
							if(pre >= 0) {
								sb.Append("\r\n");
								for(j = 0;j <= pre;j++) sb.Append("  ");
							}
						}
						else dl = true;
						sb.Append('"');
						Escape(key, sb);
						sb.Append("\":");
						map[key].ToString(sb, pre >= 0 ? (pre + 1) : -1);
					}
					if(pre >= 0) {
						sb.Append("\r\n");
						for(j = 1;j <= pre;j++) sb.Append("  ");
					}
					sb.Append('}');
					break;
				case NodeType.String:
					sb.Append('"');
					Escape((string)oval, sb);
					sb.Append('"');
					break;
				case NodeType.Null:
					sb.Append("null");
					break;
				case NodeType.Bool:
					sb.Append(val.boolval?"true":"false");
					break;
				case NodeType.Int:
					sb.Append(val.ival);
					break;
				case NodeType.Long:
					sb.Append(val.lval);
					break;
				case NodeType.Float:
					if(float.IsNaN(val.fval) || float.IsPositiveInfinity(val.fval) || float.IsNegativeInfinity(val.fval))
						sb.Append(0);
					else sb.Append(val.fval);
					break;
				case NodeType.Double:
					if(double.IsNaN(val.dval) || double.IsPositiveInfinity(val.dval) || double.IsNegativeInfinity(val.dval))
						sb.Append(0);
					else sb.Append(val.dval);
					break;
				case NodeType.Decimal:
					sb.Append(((decimal)oval).ToString());
					break;
				case NodeType.DateTime:
					sb.Append('"');
					if(val.timeval.Millisecond > 0)
						sb.Append(val.timeval.ToString("yyyy-MM-dd HH:mm:ss.fff"));
					else sb.Append(val.timeval.ToString("yyyy-MM-dd HH:mm:ss"));
					sb.Append('"');
					break;
				case NodeType.Expression:
					sb.Append((string)oval);
					break;
				default: sb.Append("\"\"");break;
			}
		}

		static void Escape(string s, StringBuilder sb = null) {
			if (s == null) return;
			foreach (char c in s) {
				switch (c) {
					case '\\': sb.Append("\\\\"); break;
					case '\"': sb.Append("\\\""); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					case '\b': sb.Append("\\b"); break;
					case '\f': sb.Append("\\f"); break;
					default: sb.Append(c); break;
				}
			}
		}

		public static JSONF Parse(string aJSON, bool runexp = true) {
			Stack<JSONF> stack = new Stack<JSONF>();
			JSONF ctx = new JSONF(NodeType.Null, default, null);//源码初值为null
			int i = 0;
			var sToken = new StringBuilder(64);
			string TokenName = "";
			int QuoteMode = 0;
			while (i < aJSON.Length) {
				switch (aJSON[i]) {
					case '/':
						if (QuoteMode == 0 && i + 1 < aJSON.Length) {
							if (aJSON[i + 1] == '/') {
								i += 2;
								while (i < aJSON.Length && aJSON[i] != '\n')
									i++;
								i--;
								break;
							}
							else if (aJSON[i + 1] == '*') {
								i += 2;
								while (i + 1 < aJSON.Length) {
									if (aJSON[i] == '*' && aJSON[i + 1] == '/') {
										i += 2;
										break;
									}
									i++;
								}
								i--;
								break;
							}
						}
						sToken.Append(aJSON[i]);
						break;
					case '{':
						if (QuoteMode == 1) {
							sToken.Append(aJSON[i]);
							break;
						}
						stack.Push(newObject());
						if (ctx.type != NodeType.Null) {
							TokenName = TokenName.Trim();
							if (ctx.type == NodeType.Array)
								ctx.Add(stack.Peek());
							else if (TokenName != "")
								ctx.Add(TokenName, stack.Peek());
						}
						TokenName = "";
						sToken.Clear();
						QuoteMode = 0;
						ctx = stack.Peek();
						break;
					case '[':
						if (QuoteMode == 1) {
							sToken.Append(aJSON[i]);
							break;
						}
						stack.Push(newArray());
						if (ctx.type != NodeType.Null) {
							TokenName = TokenName.Trim();
							if (ctx.type == NodeType.Array)
								ctx.Add(stack.Peek());
							else if (TokenName != "")
								ctx.Add(TokenName, stack.Peek());
						}
						TokenName = "";
						sToken.Clear();
						QuoteMode = 0;
						ctx = stack.Peek();
						break;
					case '}':
					case ']':
						if (QuoteMode == 1) {
							sToken.Append(aJSON[i]);
							break;
						}
						if(stack.Count == 0)
							return NOTEXIST;
						stack.Pop();
						TokenName = TokenName.Trim();
						if (sToken.Length > 0 || QuoteMode == 2) {
							if (ctx.type == NodeType.Array)
								ctx.Add(t1(sToken.ToString(), QuoteMode, runexp));
							else
								ctx.Add(TokenName, t1(sToken.ToString(), QuoteMode, runexp));
						}
						TokenName = "";
						sToken.Clear();
						QuoteMode = 0;
						if (stack.Count > 0)
							ctx = stack.Peek();
						break;
					case ':':
						if (QuoteMode == 1) {
							sToken.Append(aJSON[i]);
							break;
						}
						TokenName = sToken.ToString();
						sToken.Clear();
						QuoteMode = 0;
						break;
					case '"':
						if (QuoteMode == 0) QuoteMode = 1;
						else QuoteMode = 2;
						break;
					case ',':
						if (QuoteMode == 1) {
							sToken.Append(aJSON[i]);
							break;
						}
						TokenName = TokenName.Trim();
						if (sToken.Length > 0 || QuoteMode == 2) {
							if (ctx.type == NodeType.Array)
								ctx.Add(t1(sToken.ToString(), QuoteMode, runexp));
							else if (TokenName != "")
								ctx.Add(TokenName, t1(sToken.ToString(), QuoteMode, runexp));
						}
						TokenName = "";
						sToken.Clear();
						QuoteMode = 0;
						break;
					case '\r':
					case '\n':
						break;
					case ' ':
					case '\t':
						if (QuoteMode == 1)
							sToken.Append(aJSON[i]);
						break;
					case '\\':
						++i;
						if (QuoteMode == 1) {
							char C = aJSON[i];
							switch (C) {
								case 't': sToken.Append('\t'); break;
								case 'r': sToken.Append('\r'); break;
								case 'n': sToken.Append('\n'); break;
								case 'b': sToken.Append('\b'); break;
								case 'f': sToken.Append('\f'); break;
								case 'u': {
										string s = aJSON.Substring(i + 1, 4);
										try {
											sToken.Append((char)int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier));
										}
										catch { }
										i += 4;
										break;
									}
								default: sToken.Append(C); break;
							}
						}
						break;
					default:
						sToken.Append(aJSON[i]);
						break;
				}
				++i;
			}
			if (QuoteMode == 1) {
				return "";//throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
			}
			if (ctx.type == NodeType.Null)
				ctx.Value = sToken.ToString();
			return ctx;
		}

		public void ParseExpression() {
			if (type == NodeType.Expression) {
				type = NodeType.String;
				string sval = (string)oval;
				switch (sval) {
					case "true": type = NodeType.Bool; val.boolval = true; break;
					case "false": type = NodeType.Bool; val.boolval = false; break;
					case "null": type = NodeType.Null; break;
					case "NaN": type = NodeType.Float; val.fval = float.NaN; break;
					case "Inf": type = NodeType.Float; val.fval = float.PositiveInfinity; break;
					case "-Inf": type = NodeType.Float; val.fval = float.NegativeInfinity; break;
					default:
						string svaltrim = sval.Trim();
						if (int.TryParse(svaltrim, out int ival)) {
							type = NodeType.Int;
							val.ival = ival;
						}
						else if (long.TryParse(svaltrim, out long lval)) {
							type = NodeType.Long;
							val.lval = lval;
						}
						else if (double.TryParse(svaltrim, out double dval)) {
							type = NodeType.Double;
							val.dval = dval;
						}
						else {
							return;
						}
						break;
				}
				oval = null;
			}
		}

		static JSONF t1(string s, int quotemode = 0, bool runexp = false) {
			if(quotemode != 0)
				return s;
			switch (s) {
				case "true": return true;
				case "false": return false;
				case "null": return NULL;
				default:
					var d = new JSONF(NodeType.Expression, default, s);
					if (runexp) d.ParseExpression();
					return d;
			}
		}

		public enum TypeEnum {
			Undefined = 0,
			Null = 1,
			Bool = 2,
			String = 3,
			Byte = 4,
			SByte = 5,
			Short = 6,
			UShort = 7,
			Int = 8,
			UInt = 9,
			Long = 10,
			ULong = 11,
			Float = 12,
			Double = 13,
			Decimal = 14,
			Char = 15,
			Datetime = 16,
		}
		public static Dictionary<Type, TypeEnum> TYPEENUMMAP = new Dictionary<Type, TypeEnum> {
			{typeof(bool),TypeEnum.Bool},
			{typeof(string),TypeEnum.String},
			{typeof(byte),TypeEnum.Byte},
			{typeof(sbyte),TypeEnum.SByte},
			{typeof(short),TypeEnum.Short},
			{typeof(ushort),TypeEnum.UShort},
			{typeof(int),TypeEnum.Int},
			{typeof(uint),TypeEnum.UInt},
			{typeof(long),TypeEnum.Long},
			{typeof(ulong),TypeEnum.ULong},
			{typeof(float),TypeEnum.Float},
			{typeof(double),TypeEnum.Double},
			{typeof(decimal),TypeEnum.Decimal},
			{typeof(char),TypeEnum.Char},
			{typeof(DateTime),TypeEnum.Datetime},
		};
		public static JSONF ToJSONData(object o) {
			if(o == null) return NULL;
			if(TYPEENUMMAP.TryGetValue(o.GetType(), out var typ)) {
				switch(typ) {
					case TypeEnum.Byte: return (byte)o;
					case TypeEnum.SByte: return (sbyte)o;
					case TypeEnum.Short: return (short)o;
					case TypeEnum.UShort: return (ushort)o;
					case TypeEnum.Int: return (int)o;
					case TypeEnum.UInt: return (uint)o;
					case TypeEnum.Long: return (long)o;
					case TypeEnum.ULong: return ((ulong)o).ToString();
					case TypeEnum.Float: return (float)o;
					case TypeEnum.Double: return (double)o;
					case TypeEnum.Decimal: return (decimal)o;
					case TypeEnum.Datetime: return (DateTime)o;
					case TypeEnum.String: return (string)o;
					case TypeEnum.Bool: return (bool)o;
				}
			}
			return NULL;
		}

		public object GetValueObject() {
			switch(type) {
				case NodeType.Int: return val.ival;
				case NodeType.Long: return val.lval;
				case NodeType.Float: return val.fval;
				case NodeType.Double: return val.dval;
				case NodeType.DateTime: return val.timeval;
				case NodeType.Bool: return val.boolval;
				default: return oval;
			}
		}
	}

	public class RefList<T> : IEnumerable<T> {
		public T[] arr;
		int itemcount;

		public RefList(int capacity = 0) {
			if(capacity <= 0) capacity = 4;
			arr = new T[capacity];
		}

		public void Add(ref T t) {
			if(itemcount == arr.Length)
				Capacity = arr.Length * 2;
			arr[itemcount++] = t;
		}

		public IEnumerator<T> GetEnumerator() {
			for(int i=0;i<itemcount;i++)
				yield return arr[i];
		}

		IEnumerator IEnumerable.GetEnumerator() {
			for(int i=0;i<itemcount;i++)
				yield return arr[i];
		}

		public void Add(T t) {
			if(itemcount == arr.Length)
				Capacity = arr.Length * 2;
			arr[itemcount++] = t;
		}

		public T[] ToArray() {
			T[] list = new T[itemcount];
			Array.Copy(arr, 0, list, 0, itemcount);
			return list;
		}

		public void AddRange(IEnumerable<T> list) {
			int listlen = list.Count();
			if(itemcount + listlen > arr.Length) {
				int newlen = arr.Length * 2;
				while(itemcount + listlen > newlen)
					newlen *= 2;
				Capacity = newlen;
			}
			foreach(T v in list)
				arr[itemcount++] = v;
		}

		public void Reverse(int index = 0, int count = -1) {
			if(index < 0) index = 0;
			if(count < 0) count = itemcount - index;
			int last = Math.Min(itemcount - 1, index + count - 1);
			while(index < last) {
				T tmp = arr[index];
				arr[index] = arr[last];
				arr[last] = tmp;
				index++;
				last--;
			}
		}

		class MyCompare : IComparer<T> {
			Comparison<T> com;
			public MyCompare(Comparison<T> com) {
				this.com = com;
			}
			public int Compare(T x, T y) => com(x, y);
		}

		public void Sort(Comparison<T> com = null) {
			Array.Sort(arr, 0, itemcount, new MyCompare(com));
		}

		public void RemoveRange(int index, int count) {
			if(index < 0 || index >= itemcount) return;
			count = Math.Min(count, itemcount - index);
			int i, len;
			for(i = 0, len = itemcount - index - count;i < len;i++)
				arr[index + i] = arr[index + i + count];
			for(;i < count;i++)
				arr[index + i] = default;
			itemcount -= count;
		}

		public void RemoveAt(int index) {
			RemoveRange(index, 1);
		}

		public int Count {
			get => itemcount;
		}

		public int Capacity {
			get => arr.Length;
			set {
				if(value > arr.Length) {
					T[] newarr = new T[value];
					Array.Copy(arr, 0, newarr, 0, arr.Length);
					arr = newarr;
				}
			}
		}

		public void Clear() {
			itemcount = 0;
			int i;
			for(i = 0;i < arr.Length;i++)
				arr[i] = default;
		}
		public ref T this[int index] => ref arr[index];
	}

	public struct Pair<K, V> {
		public K key;
		public V value;
		public Pair(K key, V value) {
			this.key = key;
			this.value = value;
		}
	}

	public class RefDict<K, V> : IEnumerable<Pair<K,V>> {
		public Dictionary<K, int> posmap = new Dictionary<K, int>();
		public RefList<V> items = new RefList<V>();
		public List<int> emptypos = new List<int>();
		public ref V this[K index] => ref items[posmap[index]];

		public RefDict(int capacity = 0, IEqualityComparer<K> scmp = null) {
			if(capacity > 0 || scmp != null) posmap = new Dictionary<K, int>(capacity, scmp);
		}

		public bool TryGetValue(K index, out V value) {
			value = default;
			if(posmap.TryGetValue(index, out int pos)) {
				value = items[pos];
				return true;
			}
			return false;
		}

		public int Capacity {
			get => items.Capacity;
			set { items.Capacity = value; }
		}

		public int Count => posmap.Count;

		public void Clear() {
			posmap.Clear();
			items.Clear();
			emptypos.Clear();
		}

		public bool ContainsKey(K index) => posmap.ContainsKey(index);

		public V tryGet(K index, V value = default) {
			if(posmap.TryGetValue(index, out int pos))
				return items[pos];
			return default;
		}

		public void Add(K index, V value) {
			Add(index, ref value);
		}

		public void Add(K index, ref V value) {
			if(posmap.TryGetValue(index, out int pos))
				items[pos] = value;
			else if(emptypos.Count > 0) {
				posmap[index] = pos = emptypos[emptypos.Count - 1];
				items[pos] = value;
				emptypos.Remove(emptypos.Count - 1);
			}
			else {
				posmap[index] = items.Count;
				items.Add(ref value);
			}
		}

		public bool Remove(K index) {
			if(posmap.TryGetValue(index,out int pos)) {
				items[pos] = default;
				emptypos.Add(pos);
				posmap.Remove(index);
				return true;
			}
			return false;
		}

		public IEnumerator<Pair<K, V>> GetEnumerator() {
			foreach(var v in posmap)
				yield return new Pair<K, V>(v.Key, items[v.Value]);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			foreach(var v in posmap)
				yield return new Pair<K, V>(v.Key, items[v.Value]);
		}
	}

	[StructLayout(LayoutKind.Explicit,Size=8)]
	public struct DWord {
		//bool
		[FieldOffset(0)] public bool boolval;
		//u8[8]
		[FieldOffset(0)] public byte u8val;
		//i8[8]
		[FieldOffset(0)] public sbyte i8val;
		//u16[4]
		[FieldOffset(0)] public ushort u16val;
		//i16[4]
		[FieldOffset(0)] public short i16val;
		//u32[2]
		[FieldOffset(0)] public uint uval;
		//i32[2]
		[FieldOffset(0)] public int ival;
		[FieldOffset(4)] public int ival1;
		//u64
		[FieldOffset(0)] public ulong ulval;
		//i64
		[FieldOffset(0)] public long lval;
		//f32[2]
		[FieldOffset(0)] public float fval;
		//f64
		[FieldOffset(0)] public double dval;
		//datetime
		[FieldOffset(0)] public DateTime timeval;
		public static DWord make(byte b) => new DWord { lval = b };
		public static DWord make(sbyte b) => new DWord { lval = b };
		public static DWord make(short b) => new DWord { lval = b };
		public static DWord make(ushort b) => new DWord { lval = b };
		public static DWord make(int b) => new DWord { lval = b };
		public static DWord make(uint b) => new DWord { lval = b };
		public static DWord make(long b) => new DWord { lval = b };
		public static DWord make(ulong b) => new DWord { ulval = b };
		public static DWord make(float b) => new DWord { fval = b };
		public static DWord make(double b) => new DWord { dval = b };
		public static DWord make(DateTime b) => new DWord { timeval = b };
	}
}