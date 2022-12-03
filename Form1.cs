using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Q {
	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e) {
			emsg.Text = JSON.newObject(
				("name", "abc"),
				("height", 1.77),
				("email", "123212331212"),
				("contacts", JSON.newArray(
					"contact 1",
					"contact 2",
					"contact 3"
				))
			).ToString(0);
		}

		private void button2_Click(object sender, EventArgs e) {
			var js = JSON.Parse(einput.Text);
			var sb = new StringBuilder();
			if(js.type == JSON.NodeType.Array) {
				foreach(var v in js.Vals)
					sb.Append('[').Append(v.type.ToString()).Append("] ").Append(v.Value).Append("\r\n");
			}
			else if(js.type == JSON.NodeType.Object) {
				foreach(var (k,v) in js.KeyVals)
					sb.Append(k).Append(" = ").Append('[').Append(v.type.ToString()).Append("] ").Append(v.Value).Append("\r\n");
			}
			sb.Append("\r\njs[2].list[1] = ").Append(js[2]["list"][1]);
			emsg.Text = sb.ToString();
		}

		private void button3_Click(object sender, EventArgs e) {
			var js = JSON.newArray(1, 1234, 12345, 1234567890, 12.333f, 0, new DateTime(2022, 1, 1, 10, 11, 12));
			var b = JSONB.GenBytes(js);
			var s = js.ToString();
			emsg.Text = b.Length + ": " +JSON.bin2hex(b) + "\r\n" + s.Length + ": " + s;
		}
	}
}
