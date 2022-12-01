# CSharp Fact JSON Library

JSON parse and generate library in csharp. Features:
- Single struct implementation instead of using classes to improve memory efficiency and data access speed.

## Install
1. Copy JSONF.cs
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
js.ToString(0); //indended
```

3. Access JSON fields
```cs
MessageBox.Show(js["name"].Value);
MessageBox.Show(js["height"].AsFloat.ToString());
MessageBox.Show(js["contacts"].Count);
foreach(var v in js["contacts"].Vals)
	MessageBox.Show(v.Value);
```

4. Modify fields
```cs
js["memo"] = "abc"; //new field
js["name"] = 99999; //existing field
js["lasttime"] = DateTime.Now;
js["contacts"].Clear();
js["contacts"].Add("Item 1");
```

5. Parse JSON string
```cs
var js = JSON.Parse("[1,2,true,false,null,{},[[1]]]");
```
