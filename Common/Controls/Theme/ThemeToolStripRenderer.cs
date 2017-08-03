﻿using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml.Serialization;
using Common.Controls.Scaling;
using Common.Resources;
using Common.Resources.Properties;

namespace Common.Controls.Theme
{
	//used to render all tool/status/menu strips
	public class ThemeToolStripRenderer : ToolStripProfessionalRenderer
	{
		public ThemeToolStripRenderer() : base(new ThemeColorTable()) { }

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			e.TextColor = ThemeColorTable.ForeColor;
			base.OnRenderItemText(e);
		}
		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			ToolStripItem toolStripItem = e.Item;
			//e.ArrowColor = toolStripItem.Enabled ? ThemeColorTable.ForeColor : SystemColors.ControlDark;
			if (toolStripItem is ToolStripDropDownItem)
			{
				Rectangle r = e.ArrowRectangle;
				List<Point> points = new List<Point>();
				points.Add(new Point(r.Left - 2, r.Height / 2 - 3));
				points.Add(new Point(r.Right + 2, r.Height / 2 - 3));
				points.Add(new Point(r.Left + (r.Width / 2),
					r.Height / 2 + 3));
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				e.Graphics.FillPolygon(new SolidBrush(ThemeColorTable.ForeColor), points.ToArray());
				e.ArrowColor = toolStripItem.Enabled ? ThemeColorTable.ButtonTextColor : SystemColors.ControlDark;

			}
			else
			{
				base.OnRenderArrow(e);
			}
		}

		protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
		{
			var item = e.Item;
			Graphics g = e.Graphics;
			Rectangle bounds = new Rectangle(Point.Empty, item.Size);


			if (e.Item.Selected)
			{
				RenderSelectedButtonFill(bounds, g);
			}
			else
			{
				using (Brush b = new SolidBrush(item.BackColor))
				{
					g.FillRectangle(b, bounds);
				}
			}
		}

		protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
		{
			var item = e.Item;
			Graphics g = e.Graphics;
			Rectangle bounds = new Rectangle(Point.Empty, item.Size);

			
			if (e.Item.Selected)
			{
				RenderSelectedButtonFill(bounds, g);
			}
			else
			{
				Color fillColor = ThemeColorTable.BackgroundColor;
				using (Brush b = new SolidBrush(fillColor))
				{
					g.FillRectangle(b, bounds);
				}
			}

		}

		protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
		{
			//base.OnRenderButtonBackground(e);
			ToolStripButton item = e.Item as ToolStripButton;
			Graphics g = e.Graphics;
			Rectangle bounds = new Rectangle(Point.Empty, item.Size);

			if (item.CheckState == CheckState.Checked)
			{
				RenderCheckedButtonFill(bounds, item, g);
			}
			else
			{
				if (item.Selected)
				{
					RenderSelectedButtonFill(bounds, g);
				}
				else
				{
					Color fillColor = ThemeColorTable.BackgroundColor;
					using (Brush b = new SolidBrush(fillColor))
					{
						g.FillRectangle(b, bounds);
					}
				}
				
			}

		}

		private void RenderSelectedButtonFill(Rectangle bounds, Graphics g)
		{
			GraphicsPath path = new GraphicsPath();
			path.AddRectangle(new RectangleF(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1));
			using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
			{
				// Set the color at the center of the path.
				pthGrBrush.CenterColor = ColorTable.ButtonSelectedGradientBegin;

				Color[] colors = { ColorTable.ButtonSelectedGradientEnd };
				pthGrBrush.SurroundColors = colors;

				g.FillRectangle(pthGrBrush, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
			}
		}

		private void RenderCheckedButtonFill(Rectangle bounds, ToolStripButton item, Graphics g)
		{
			if (item.Selected)
			{
				RenderSelectedButtonFill(bounds, g);
			}
			
			// Set the color of border when item is selected.
			Pen pen = new Pen(ThemeColorTable.ForeColor);
			g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
			
		}

		protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
		{
			base.OnRenderItemBackground(e);
			Graphics g = e.Graphics;
			Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
			if (e.Item.Selected)
			{
				using (Pen p = new Pen(ThemeColorTable.HighlightColor, 1))
				{
					g.DrawRectangle(p, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
				}
			}
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			int iconSize = (int)(24 * ScalingTools.GetScaleFactor());
			Image image = Tools.GetIcon(Resources.Properties.Resources.check_mark, iconSize);
			Rectangle imageRect = e.ImageRectangle;


			if (imageRect != Rectangle.Empty && image != null)
			{
				if (!e.Item.Enabled)
				{
					image = CreateDisabledImage(image);
				}

				e.Graphics.DrawImage(image, imageRect, new Rectangle(Point.Empty, imageRect.Size), GraphicsUnit.Pixel);
			}

		}
	}
}
