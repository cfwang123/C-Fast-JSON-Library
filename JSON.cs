using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace Q {

	public struct JSON : IEnumerable<JSON> {
		public static JSON NOTEXIST = new JSON(NodeType.NotExist, default, null);

		public enum NodeType { NotExist = 0, Null, Array, Object, Int, Long, Float, Double, DateTime, Bool, Decimal, String, ByteArray, CustomObject, Expression }
		public NodeType type;
		public DWord val; //i32 i64 float double datetime
		public object oval;
		public JSON(NodeType type, DWord val, object oval) {
			this.type = type;
			this.val = val;
			this.oval = oval;
		}

		public bool IsNull => type == NodeType.Null || type == NodeType.NotExist;

		public ref JSON this[int index] {
			get {
				if(oval is RefList<JSON> arr) {
					if(index >= 0 && index < arr.Count)
						return ref arr[index];
				}
				return ref NOTEXIST;
			}
		}

		public ref JSON this[string key] {
			get {
				if(oval is RefDict<string,JSON> arr) {
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

		public ref JSON get(string key) {
			if(type == NodeType.Object) {
				var arr = oval as RefDict<string,JSON>;
				if(arr.posmap.TryGetValue(key, out int pos))
					return ref arr.items[pos];
			}
			return ref NOTEXIST;
		}

		public int Count {
			get {
				if(oval is RefList<JSON> arr) return arr.Count;
				else if(oval is RefDict<string, JSON> obj) return obj.Count;
				else return 0;
			}
		}

		public void Add(string key, JSON value) {
			if(type != NodeType.Object) return;
			var map = oval as RefDict<string, JSON>;
			if(map.posmap.TryGetValue(key, out int pos))
				map.items[pos] = value;
			else map.Add(key, value);
		}

		public void Add(string key, ref JSON value) {
			if(type != NodeType.Object) return;
			var map = oval as RefDict<string, JSON>;
			if(map.posmap.TryGetValue(key, out int pos))
				map.items[pos] = value;
			else map.Add(key, value);
		}

		public void Add(JSON value) {
			if(type != NodeType.Array) return;
			var arr = oval as RefList<JSON>;
			arr.Add(value);
		}

		public void Add(ref JSON value) {
			if(type != NodeType.Array) return;
			var arr = oval as RefList<JSON>;
			arr.Add(value);
		}

		public void Clear() {
			if(oval is RefList<JSON> arr) arr.Clear();
			else if(oval is RefDict<string, JSON> obj) obj.Clear();
		}

		public void EnsureCapacity(int capacity) {
			if(oval is RefList<JSON> arr) arr.Capacity = capacity;
			else if(oval is RefDict<string, JSON> obj) obj.Capacity = capacity;
		}

		public string Value {
			get {
				ParseExpression();
				switch(type) {
					case NodeType.String: return (string)oval;
					case NodeType.Int: return val.ival.ToString();
					case NodeType.Long: return val.lval.ToString();
					case NodeType.Float:
						if(float.IsNaN(val.fval)) return "NaN";
						else if(float.IsPositiveInfinity(val.fval)) return "Inf";
						else if(float.IsNegativeInfinity(val.fval)) return "-Inf";
						else return val.fval.ToString();
					case NodeType.Double:
						if(double.IsNaN(val.dval)) return "NaN";
						else if(double.IsPositiveInfinity(val.dval)) return "Inf";
						else if(double.IsNegativeInfinity(val.dval)) return "-Inf";
						else return val.dval.ToString();
					case NodeType.Bool: return val.boolval ? "1" : "0";
					case NodeType.DateTime: return val.timeval.ToString("yyyy-MM-dd HH:mm:ss");
					case NodeType.Decimal: return ((decimal)oval).ToString();
					case NodeType.ByteArray: return bin2hex((byte[])oval);
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
				oval = null;
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
				oval = null;
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
				}			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Float;
				val.fval = value;
				oval = null;
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
				oval = null;
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

		static readonly DateTime EPOCH = new DateTime(1970, 1, 1);
		public DateTime AsDateTime {
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
				oval = null;
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
					case NodeType.Array: return (oval as RefList<JSON>).Count > 0;
					case NodeType.Object: return (oval as RefDict<string,JSON>).Count > 0;
					default: return false;
				}
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.Bool;
				val.boolval = value;
				oval = null;
			}
		}

		public byte[] AsByteArray {
			get {
				if(type == NodeType.ByteArray)
					return (byte[])oval;
				else return new byte[0];
			}
			set {
				if(type == NodeType.NotExist) return;
				type = NodeType.ByteArray;
				oval = value;
			}
		}

		public JSON AsObject {
			get {
				if(type == NodeType.Object)
					return this;
				else return newObject();
			}
		}

		public JSON AsArray {
			get {
				if(type == NodeType.Array)
					return this;
				else return newArray();
			}
		}

		public static JSON newArray(int capacity = 0) {
			return new JSON(NodeType.Array, default, new RefList<JSON>(capacity));
		}

		public static JSON newObject(int capacity = 0, StringComparer scmp = null) {
			return new JSON(NodeType.Object, default, new RefDict<string, JSON>(capacity, scmp));
		}

		public static JSON newArray(params JSON[] items) {
			var list = new RefList<JSON>(items.Length);
			int i;
			for(i = 0;i < items.Length - 1;i++)
				list.Add(ref items[i]);
			if(items.Length > 0 && items[i].type != NodeType.NotExist)
				list.Add(ref items[i]);
			return new JSON(NodeType.Array, default, list);
		}

		public static JSON newObject(params (string, JSON)[] items) {
			var map = new RefDict<string, JSON>();
			int i;
			for(i = 0;i < items.Length - 1;i++)
				map.Add(items[i].Item1, ref items[i].Item2);
			if(items.Length > 0 && items[i].Item1 != null && items[i].Item2.type != NodeType.NotExist)
				map.Add(items[i].Item1, ref items[i].Item2);
			return new JSON(NodeType.Object, default, map);
		}
		public static JSON NULL => new JSON(NodeType.Null, default, null);
		public static JSON newData(int v) => new JSON(NodeType.Int, DWord.make(v), null);
		public static JSON newData(long v) => new JSON(NodeType.Long, DWord.make(v), null);
		public static JSON newData(float v) => new JSON(NodeType.Float, DWord.make(v), null);
		public static JSON newData(double v) => new JSON(NodeType.Double, DWord.make(v), null);
		public static JSON newData(DateTime v) => new JSON(NodeType.DateTime, DWord.make(v), null);
		public static JSON newData(bool v) => new JSON(NodeType.Bool, DWord.make(v), null);
		public static JSON newData(decimal v) => new JSON(NodeType.Decimal, default, v);
		public static JSON newData(string v) => new JSON(v==null ? NodeType.Null : NodeType.String, default, v);
		public static JSON newData(byte[] v) => new JSON(NodeType.ByteArray, default, v);
		public static JSON newCustomData(object v) => new JSON(NodeType.CustomObject, default, v);

		public static implicit operator JSON(bool b) => JSON.newData(b);
		public static implicit operator JSON(int b) => JSON.newData(b);
		public static implicit operator JSON(long b) => JSON.newData(b);
		public static implicit operator JSON(float b) => JSON.newData(b);
		public static implicit operator JSON(double b) => JSON.newData(b);
		public static implicit operator JSON(DateTime b) => JSON.newData(b);
		public static implicit operator JSON(decimal b) => JSON.newData(b);
		public static implicit operator JSON(string b) => JSON.newData(b);
		public static implicit operator JSON(byte[] b) => JSON.newData(b);
		public static implicit operator string(JSON b) => b.Value;

		public IEnumerator<JSON> GetEnumerator() {
			if(oval is RefList<JSON> arr) {
				foreach(var v in arr)
					yield return v;
			}
			else if(oval is RefDict<string, JSON> obj) {
				foreach(var v in obj)
					yield return v.Item2;
			}
		}

		public IEnumerable<JSON> Vals {
			get {
				if(oval is RefList<JSON> arr) {
					foreach(var v in arr)
						yield return v;
				}
				else if(oval is RefDict<string, JSON> obj) {
					foreach(var v in obj)
						yield return v.Item2;
				}
			}
		}

		public IEnumerable<string> Keys {
			get {
				if(oval is RefDict<string, JSON> obj) {
					foreach(var v in obj)
						yield return v.Item1;
				}
			}
		}

		public IEnumerable<(string,JSON)> KeyVals {
			get {
				if(oval is RefDict<string, JSON> obj) {
					foreach(var v in obj)
						yield return v;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			if(oval is RefList<JSON> arr) {
				foreach(var v in arr)
					yield return v;
			}
			else if(oval is RefDict<string, JSON> obj) {
				foreach(var v in obj)
					yield return v.Item2;
			}
		}

		[ThreadStatic] static StringBuilder _sb;
		public override string ToString() {
			return ToString(-1);
		}

		public string ToString(int pre = -1) {
			if(type == NodeType.Array || type == NodeType.Object) {
				_sb = _sb ?? new StringBuilder(2000);
				ToJSONString(_sb, pre);
				string s = _sb.ToString();
				_sb.Clear();
				return s;
			}
			else return Value;
		}

		public string ToJSONString(int pre = -1) {
			_sb = _sb ?? new StringBuilder(2000);
			ToJSONString(_sb, pre);
			string s = _sb.ToString();
			_sb.Clear();
			return s;
		}

		public void ToJSONString(StringBuilder sb, int pre = -1) {
			bool dl = false;
			int i, j, len;
			switch(type) {
				case NodeType.Array:
					var arr = oval as RefList<JSON>;
					if(arr.Count == 0) {
						sb.Append("[]");
						break;
					}
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
						arr[i].ToJSONString(sb, pre >= 0 ? (pre + 1) : -1);
					}
					if(pre >= 0) {
						sb.Append("\r\n");
						for(j = 1;j <= pre;j++) sb.Append("  ");
					}
					sb.Append(']');
					break;
				case NodeType.Object:
					var map = oval as RefDict<string,JSON>;
					if(map.Count == 0) {
						sb.Append("{}");
						break;
					}
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
						map[key].ToJSONString(sb, pre >= 0 ? (pre + 1) : -1);
					}
					if(pre >= 0) {
						sb.Append("\r\n");
						for(j = 1;j <= pre;j++) sb.Append("  ");
					}
					sb.Append('}');
					break;
				case NodeType.ByteArray:
					sb.Append('"');
					sb.Append(bin2hex((byte[])oval));
					sb.Append('"');
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
					if(float.IsNaN(val.fval))
						sb.Append("\"NaN\"");
					else if(float.IsPositiveInfinity(val.fval))
						sb.Append("\"Inf\"");
					else if(float.IsNegativeInfinity(val.fval))
						sb.Append("\"-Inf\"");
					else sb.Append(val.fval);
					break;
				case NodeType.Double:
					if(double.IsNaN(val.dval))
						sb.Append("\"NaN\"");
					else if(double.IsPositiveInfinity(val.dval))
						sb.Append("\"Inf\"");
					else if(double.IsNegativeInfinity(val.dval))
						sb.Append("\"-Inf\"");
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

		public static string bin2hex(byte[] bytes, int start = 0, int length = -1) {
			if (bytes == null) return "NULL";
			StringBuilder sb = new StringBuilder(length >= 0 ? length * 2 : bytes.Length * 2);
			int end = length >= 0 ? Math.Min(bytes.Length, start + length) : bytes.Length;
			char c1, c2;
			byte b;
			uint value;
			for (int i = start; i < end; i++) {
				b = bytes[i];
				value = (uint)b % 16;
				if (value < 10)
					c2 = (char)('0' + value);
				else c2 = (char)('A' + value - 10);
				value = (uint)b / 16 % 16;
				if (value < 10)
					c1 = (char)('0' + value);
				else c1 = (char)('A' + value - 10);
				sb.Append(c1).Append(c2);
			}
			return sb.ToString();
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

		public static JSON Parse(string aJSON, bool runexp = true) {
			Stack<JSON> stack = new Stack<JSON>();
			JSON ctx = new JSON(NodeType.Null, default, null);//源码初值为null
			int i = 0;
			StringBuilder sToken = new StringBuilder(64);
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
							if(ctx.oval is RefList<JSON> arr)
								arr.Add(stack.Peek());
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
							if(ctx.oval is RefList<JSON> arr)
								arr.Add(stack.Peek());
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
							if(ctx.oval is RefList<JSON> arr)
								arr.Add(t1(sToken.ToString(), QuoteMode, runexp));
							else ctx.Add(TokenName, t1(sToken.ToString(), QuoteMode, runexp));
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
							if(ctx.oval is RefList<JSON> arr)
								arr.Add(t1(sToken.ToString(), QuoteMode, runexp));
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

		static JSON t1(string s, int quotemode = 0, bool runexp = false) {
			if(quotemode != 0)
				return s;
			switch (s) {
				case "true": return true;
				case "false": return false;
				case "null": return NULL;
				default:
					var d = new JSON(NodeType.Expression, default, s);
					if (runexp) d.ParseExpression();
					return d;
			}
		}

		static HashSet<Type> __JSONDataTypes = new HashSet<Type> {
			typeof(byte),
			typeof(sbyte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(decimal),
			typeof(DateTime),
			typeof(bool),
			typeof(string),
			typeof(byte[]),
		};
		public static bool IsJSONDataType(object o) {
			if (o == null) return true;
			return __JSONDataTypes.Contains(o.GetType());
		}
		public static JSON ToJSONData(object o) {
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
					case TypeEnum.ByteArray: return new JSON(NodeType.ByteArray, default, (byte[])o);
				}
			}
			return NULL;
		}
		enum TypeEnum {
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
			ByteArray = 16,
			Datetime = 17,
		}
		static Dictionary<Type, TypeEnum> TYPEENUMMAP = new Dictionary<Type, TypeEnum> {
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
			{typeof(byte[]),TypeEnum.ByteArray},
			{typeof(DateTime),TypeEnum.Datetime},
		};

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

	public class RefDict<K, V> : IEnumerable<(K,V)> {
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

		public IEnumerator<(K, V)> GetEnumerator() {
			foreach(var v in posmap)
				yield return (v.Key, items[v.Value]);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			foreach(var v in posmap)
				yield return (v.Key, items[v.Value]);
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
		public static DWord make(bool b) => new DWord { boolval = b };
	}
}
