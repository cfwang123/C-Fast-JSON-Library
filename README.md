# CSharp Fact JSON Library

JSON parse and generate library in csharp.
This JSON library an be used to parse, generate JSON string. It can also be used as a very flexible data structure, for example to store a database result set.

Features:
- Single struct implementation instead of using classes
- Memory efficient, no boxing for integer and float values.
- Fast speed
- Convert JSON struct to a very compact binary format, and parse JSON struct in binary data.

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

2. Generate JSON string
```cs
js.ToString(); //compact
js.ToString(0); //indented
```

3. Access JSON fields
```cs
MessageBox.Show(js["name"].Value);
MessageBox.Show(js["height"].AsFloat.ToString());
MessageBox.Show(js["height"].AsInt.ToString());
MessageBox.Show(js["contacts"].Count);
foreach(var v in js["contacts"].Vals)
	MessageBox.Show(v.Value);
MessageBox.Show(js["aaa"].Value); //not existing field return default values(empty string)
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
```
