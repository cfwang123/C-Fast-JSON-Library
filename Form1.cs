using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Q.JSON;

namespace MyJSON {
	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e) {
			emsg.Text = JSONF.newObject(
				new Pair<string,JSONF>("name", "abc"),
				new Pair<string,JSONF>("height", 1.77),
				new Pair<string,JSONF>("email", "123212331212"),
				new Pair<string,JSONF>("contacts", JSONF.newArray(
					"contact 1",
					"contact 2",
					"contact 3"
				))
			).ToString(0);
		}

		private void button2_Click(object sender, EventArgs e) {
			var js = JSONF.Parse(einput.Text);
			var sb = new StringBuilder();
			if(js.type == JSONF.NodeType.Array) {
				foreach(var v in js.Vals)
					sb.Append('[').Append(v.type.ToString()).Append("] ").Append(v.Value).Append("\r\n");
			}
			else if(js.type == JSONF.NodeType.Object) {
				foreach(var v in js.KeyVals)
					sb.Append(v.key).Append(" = ").Append('[').Append(v.value.type.ToString()).Append("] ").Append(v.value.Value).Append("\r\n");
			}
			sb.Append("\r\njs[2].list[1] = ").Append(js[2]["list"][1]);
			emsg.Text = sb.ToString();
		}
	}
}
