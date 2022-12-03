# CSharp Fact JSON Library

JSON parse and generate library in csharp.
This JSON library an be used to parse, generate JSON string. It can also be used as a very flexible data structure, for example to store a database result set.

Features:
- Single struct implementation instead of using classes
- Memory efficient, no boxing for integer and float values. This also means less memory indirections and faster speed.
- Convert JSON struct to a very compact binary format, and parse JSON struct in binary data.

In this library, a JSON value can contain one of the following values:
1. Value types, using an 8-byte C union like structure to store:
	- Null
	- bool
	- int
	- long
	- float
	- double
	- DateTime
2. Class types, using an object field to store:
	- decimal: actually a value type using 16 bytes, cannot fit into 8-byte union, so use value boxing to store it
	- string
	- byte[]: not supported when serialized
	- JSON Array: a list of many JSON values
	- JSON Object: a dictionary of string key and JSON value
	- Arbitrary csharp object: arbitrary object will use ToString() to serialize, and when parsed, it will become a plain string.

When converted to JSON string and converted back, some value's type will be lost, for example:
- int,long,float,double,decimal will become int, long or double according to its value
- DateTime will become string
- byte[] will become string
- Arbitrary objects become string

In addition, there is another class `JSONB` for binary encoding and decoding of the JSON struct. Binary format can be used for any data communication protocols, or to store the JSON struct on disk or database. Binary format has the following advantages:
- Keep the type of every value
- Keep float and double values precisely
- Support byte[] fields
- Very compact, about half in size comparing to JSON string.

For example:
```cs
var js = JSON.newArray(1,1234,12345,1234567890,12.333f,0,new DateTime(2022,1,1,10,11,12));
JSONB.GenBytes(js); //01B333D20433393003D202964905F8534541A3080060AA090FCDD908, 28 bytes
js.ToString(); //"[1,1234,12345,1234567890,12.333,0,"2022-01-01 10:11:12"]", 56 bytes
```

## Install
1. Copy JSON.cs
2. Install dependency System.ValueTuple

## Usage
1. Generate JSON struct
```cs
var js = JSON.newObject(
	("name", "abc"),
	("height", 1.77),
	("email", "123212331212"),
	("contacts", JSON.newArray(
		"contact 1",
		"contact 2",
		"contact 3"
	))
);
```

Alternatively, you can add a `default` at the end of list, this item will be omitted. In this way every item has a comma after it.
```js
var js = JSON.newObject(
	("name", "abc"),
	("height", 1.77),
	("email", "123212331212"),
	("contacts", JSON.newArray(
		"contact 1",
		"contact 2",
		"contact 3",
		default
	)),
	("logintime", DateTime.Now), //datetime value
	("nullvalue", null),
	("truevalue", true),
	("falsevalue", false),
	default
);
```

2. Generate JSON string
```cs
js.ToString(); //compact
js.ToString(0); //indented
```

3. Access JSON struct
```cs
MessageBox.Show(js["name"].Value); //abc
MessageBox.Show(js["height"].AsFloat.ToString()); //1.77
MessageBox.Show(js["height"].AsInt.ToString()); //1
MessageBox.Show(js["contacts"].Count); //3
foreach(JSON v in js["contacts"].Vals)
	MessageBox.Show(v.Value);
foreach((string k, JSON v) in js.KeyVals)
	MessageBox.Show(k + " = " + v.Value);
MessageBox.Show(js["aaa"].Value); //"", not existing field return default values(empty string)
MessageBox.Show(js["not"]["exist"]["path"][0][0].AsInt); //0
if(js["not"]["exist"]["path"][0][0].IsNull)
	MessageBox.Show("path not exist");
```

4. Modify fields
```cs
js["memo"] = "abc"; //assign new field
js["name"] = 99999; //modify existing field to int value
js["name"].AsDecimal = 99999; //modify to decimal value
js["lasttime"] = DateTime.Now;
js["contacts"].Clear();
js["contacts"].Add("Item 1");
```

5. Parse JSON string
```cs
var js = JSON.Parse("[1,2,true,false,null,{},[[1]]]");
MessageBox.Show(js[6][0][0]);
```

6. Binary format encoding/decoding
```cs
byte[] b = JSONB.GenBytes(js);
JSON js2 = JSONB.Parse(b);

//default string encoding is GBK, use another string encoding like this
byte[] b = JSONB.GenBytes(js, JSONB.StringEncoding.UTF8); //use utf8 for string encoding
JSON js2 = JSONB.Parse(b, enc:JSONB.StringEncoding.UTF8);
```
