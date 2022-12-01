
namespace MyJSON {
	partial class Form1 {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.button1 = new System.Windows.Forms.Button();
			this.einput = new System.Windows.Forms.TextBox();
			this.emsg = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(259, 12);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "to json";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// einput
			// 
			this.einput.Location = new System.Drawing.Point(12, 41);
			this.einput.Multiline = true;
			this.einput.Name = "einput";
			this.einput.Size = new System.Drawing.Size(346, 348);
			this.einput.TabIndex = 1;
			this.einput.Text = "[1,2,{\"name\":\"fff\",\"list\":[\"item 1\", \"Item 2\"]}]";
			// 
			// emsg
			// 
			this.emsg.Location = new System.Drawing.Point(364, 41);
			this.emsg.Multiline = true;
			this.emsg.Name = "emsg";
			this.emsg.ReadOnly = true;
			this.emsg.Size = new System.Drawing.Size(346, 348);
			this.emsg.TabIndex = 1;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(12, 12);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 0;
			this.button2.Text = "parse";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(742, 401);
			this.Controls.Add(this.emsg);
			this.Controls.Add(this.einput);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox einput;
		private System.Windows.Forms.TextBox emsg;
		private System.Windows.Forms.Button button2;
	}
}

