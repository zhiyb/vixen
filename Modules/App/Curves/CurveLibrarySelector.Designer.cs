﻿namespace VixenModules.App.Curves
{
	partial class CurveLibrarySelector
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.listViewCurves = new System.Windows.Forms.ListView();
			this.buttonEditCurve = new System.Windows.Forms.Button();
			this.buttonDeleteCurve = new System.Windows.Forms.Button();
			this.buttonNewCurve = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.FlatAppearance.BorderColor = System.Drawing.Color.Black;
			this.buttonCancel.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
			this.buttonCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
			this.buttonCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
			this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
			this.buttonCancel.Location = new System.Drawing.Point(385, 338);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(80, 25);
			this.buttonCancel.TabIndex = 5;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.MouseLeave += new System.EventHandler(this.buttonBackground_MouseLeave);
			this.buttonCancel.MouseHover += new System.EventHandler(this.buttonBackground_MouseHover);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.FlatAppearance.BorderColor = System.Drawing.Color.Black;
			this.buttonOK.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
			this.buttonOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
			this.buttonOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
			this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
			this.buttonOK.Location = new System.Drawing.Point(299, 338);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(80, 25);
			this.buttonOK.TabIndex = 4;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.MouseLeave += new System.EventHandler(this.buttonBackground_MouseLeave);
			this.buttonOK.MouseHover += new System.EventHandler(this.buttonBackground_MouseHover);
			// 
			// listViewCurves
			// 
			this.listViewCurves.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewCurves.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(68)))), ((int)(((byte)(68)))), ((int)(((byte)(68)))));
			this.listViewCurves.Location = new System.Drawing.Point(-1, -1);
			this.listViewCurves.Name = "listViewCurves";
			this.listViewCurves.Size = new System.Drawing.Size(479, 327);
			this.listViewCurves.TabIndex = 6;
			this.listViewCurves.UseCompatibleStateImageBehavior = false;
			this.listViewCurves.SelectedIndexChanged += new System.EventHandler(this.listViewCurves_SelectedIndexChanged);
			this.listViewCurves.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listViewCurves_MouseDoubleClick);
			// 
			// buttonEditCurve
			// 
			this.buttonEditCurve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonEditCurve.Enabled = false;
			this.buttonEditCurve.FlatAppearance.BorderColor = System.Drawing.Color.Black;
			this.buttonEditCurve.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
			this.buttonEditCurve.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
			this.buttonEditCurve.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
			this.buttonEditCurve.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonEditCurve.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
			this.buttonEditCurve.Location = new System.Drawing.Point(98, 338);
			this.buttonEditCurve.Name = "buttonEditCurve";
			this.buttonEditCurve.Size = new System.Drawing.Size(80, 25);
			this.buttonEditCurve.TabIndex = 7;
			this.buttonEditCurve.Text = "Edit Curve";
			this.buttonEditCurve.UseVisualStyleBackColor = true;
			this.buttonEditCurve.EnabledChanged += new System.EventHandler(this.buttonTextColorChange);
			this.buttonEditCurve.Click += new System.EventHandler(this.buttonEditCurve_Click);
			this.buttonEditCurve.MouseLeave += new System.EventHandler(this.buttonBackground_MouseLeave);
			this.buttonEditCurve.MouseHover += new System.EventHandler(this.buttonBackground_MouseHover);
			// 
			// buttonDeleteCurve
			// 
			this.buttonDeleteCurve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonDeleteCurve.Enabled = false;
			this.buttonDeleteCurve.FlatAppearance.BorderColor = System.Drawing.Color.Black;
			this.buttonDeleteCurve.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
			this.buttonDeleteCurve.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
			this.buttonDeleteCurve.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
			this.buttonDeleteCurve.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonDeleteCurve.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
			this.buttonDeleteCurve.Location = new System.Drawing.Point(184, 338);
			this.buttonDeleteCurve.Name = "buttonDeleteCurve";
			this.buttonDeleteCurve.Size = new System.Drawing.Size(80, 25);
			this.buttonDeleteCurve.TabIndex = 8;
			this.buttonDeleteCurve.Text = "Delete Curve";
			this.buttonDeleteCurve.UseVisualStyleBackColor = true;
			this.buttonDeleteCurve.EnabledChanged += new System.EventHandler(this.buttonTextColorChange);
			this.buttonDeleteCurve.Click += new System.EventHandler(this.buttonDeleteCurve_Click);
			this.buttonDeleteCurve.MouseLeave += new System.EventHandler(this.buttonBackground_MouseLeave);
			this.buttonDeleteCurve.MouseHover += new System.EventHandler(this.buttonBackground_MouseHover);
			// 
			// buttonNewCurve
			// 
			this.buttonNewCurve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonNewCurve.FlatAppearance.BorderColor = System.Drawing.Color.Black;
			this.buttonNewCurve.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
			this.buttonNewCurve.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
			this.buttonNewCurve.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
			this.buttonNewCurve.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.buttonNewCurve.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
			this.buttonNewCurve.Location = new System.Drawing.Point(12, 338);
			this.buttonNewCurve.Name = "buttonNewCurve";
			this.buttonNewCurve.Size = new System.Drawing.Size(80, 25);
			this.buttonNewCurve.TabIndex = 9;
			this.buttonNewCurve.Text = "New Curve";
			this.buttonNewCurve.UseVisualStyleBackColor = true;
			this.buttonNewCurve.Click += new System.EventHandler(this.buttonNewCurve_Click);
			this.buttonNewCurve.MouseLeave += new System.EventHandler(this.buttonBackground_MouseLeave);
			this.buttonNewCurve.MouseHover += new System.EventHandler(this.buttonBackground_MouseHover);
			// 
			// CurveLibrarySelector
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(68)))), ((int)(((byte)(68)))), ((int)(((byte)(68)))));
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(477, 375);
			this.Controls.Add(this.buttonNewCurve);
			this.Controls.Add(this.buttonDeleteCurve);
			this.Controls.Add(this.buttonEditCurve);
			this.Controls.Add(this.listViewCurves);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.DoubleBuffered = true;
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(493, 414);
			this.Name = "CurveLibrarySelector";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Curve Library";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CurveLibrarySelector_FormClosing);
			this.Load += new System.EventHandler(this.CurveLibrarySelector_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CurveLibrarySelector_KeyDown);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.ListView listViewCurves;
		private System.Windows.Forms.Button buttonEditCurve;
		private System.Windows.Forms.Button buttonDeleteCurve;
		private System.Windows.Forms.Button buttonNewCurve;
	}
}