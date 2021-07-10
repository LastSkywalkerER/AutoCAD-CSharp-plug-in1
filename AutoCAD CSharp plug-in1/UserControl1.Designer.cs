
namespace AutoCAD_CSharp_plug_in1
{
	partial class UserControl1
	{
		/// <summary> 
		/// Обязательная переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором компонентов

		/// <summary> 
		/// Требуемый метод для поддержки конструктора — не изменяйте 
		/// содержимое этого метода с помощью редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.DragLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// treeView1
			// 
			this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.treeView1.Location = new System.Drawing.Point(5, 5);
			this.treeView1.Margin = new System.Windows.Forms.Padding(5);
			this.treeView1.Name = "treeView1";
			this.treeView1.Size = new System.Drawing.Size(140, 120);
			this.treeView1.TabIndex = 0;
			// 
			// DragLabel
			// 
			this.DragLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DragLabel.AutoSize = true;
			this.DragLabel.Location = new System.Drawing.Point(47, 130);
			this.DragLabel.Name = "DragLabel";
			this.DragLabel.Size = new System.Drawing.Size(56, 13);
			this.DragLabel.TabIndex = 1;
			this.DragLabel.Text = "DragLabel";
			// 
			// UserControl1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.DragLabel);
			this.Controls.Add(this.treeView1);
			this.Name = "UserControl1";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Label DragLabel;
	}
}
