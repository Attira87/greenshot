//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

#endregion

namespace Greenshot.Legacy.Controls
{
	/// <summary>
	///     Description of ColorDialog.
	/// </summary>
	public partial class ColorDialog : GreenshotForm
	{
		private static ColorDialog _uniqueInstance;

		private readonly List<Button> _colorButtons = new List<Button>();
		private readonly List<Button> _recentColorButtons = new List<Button>();
		private readonly ToolTip _toolTip = new ToolTip();
		private bool _updateInProgress;

		private ColorDialog()
		{
			SuspendLayout();
			InitializeComponent();
			CreateColorPalette(5, 5, 15, 15);
			CreateLastUsedColorButtonRow(5, 190, 15, 15);
			ResumeLayout();
			UpdateRecentColorsButtonRow();
		}

		public Color Color
		{
			get { return colorPanel.BackColor; }
			set { PreviewColor(value, this); }
		}

		/// <summary>
		///     This is a bit of a hack, but assign the list of Colors for the RecentColors to this property
		/// </summary>
		public static IList<Color> RecentColors { get; set; }

		#region helper functions

		private static int GetColorPartIntFromString(string s)
		{
			int ret = 0;
			int.TryParse(s, out ret);
			if (ret < 0)
			{
				ret = 0;
			}
			else if (ret > 255)
			{
				ret = 255;
			}
			return ret;
		}

		#endregion

		public static ColorDialog GetInstance()
		{
			return _uniqueInstance ?? (_uniqueInstance = new ColorDialog());
		}

		private void PipetteUsed(object sender, PipetteUsedArgs e)
		{
			Color = e.Color;
		}

		#region user interface generation

		private void CreateColorPalette(int x, int y, int w, int h)
		{
			CreateColorButtonColumn(255, 0, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(255, 255/2, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(255, 255, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(255/2, 255, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(0, 255, 0, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(0, 255, 255/2, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(0, 255, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(0, 255/2, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(0, 0, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(255/2, 0, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(255, 0, 255, x, y, w, h, 11);
			x += w;
			CreateColorButtonColumn(255, 0, 255/2, x, y, w, h, 11);
			x += w + 5;
			CreateColorButtonColumn(255/2, 255/2, 255/2, x, y, w, h, 11);

			Controls.AddRange(_colorButtons.ToArray());
		}

		private void CreateColorButtonColumn(int red, int green, int blue, int x, int y, int w, int h, int shades)
		{
			int shadedColorsNum = (shades - 1)/2;
			for (int i = 0; i <= shadedColorsNum; i++)
			{
				_colorButtons.Add(CreateColorButton(red*i/shadedColorsNum, green*i/shadedColorsNum, blue*i/shadedColorsNum, x, y + i*h, w, h));
				if (i > 0)
				{
					_colorButtons.Add(CreateColorButton(red + (255 - red)*i/shadedColorsNum, green + (255 - green)*i/shadedColorsNum, blue + (255 - blue)*i/shadedColorsNum, x, y + (i + shadedColorsNum)*h, w, h));
				}
			}
		}

		private Button CreateColorButton(int red, int green, int blue, int x, int y, int w, int h)
		{
			return CreateColorButton(Color.FromArgb(255, red, green, blue), x, y, w, h);
		}

		private Button CreateColorButton(Color color, int x, int y, int w, int h)
		{
			Button b = new Button();
			b.BackColor = color;
			b.FlatAppearance.BorderSize = 0;
			b.FlatStyle = FlatStyle.Flat;
			b.Location = new Point(x, y);
			b.Size = new Size(w, h);
			b.TabStop = false;
			b.Click += ColorButtonClick;
			_toolTip.SetToolTip(b, ColorTranslator.ToHtml(color) + " | R:" + color.R + ", G:" + color.G + ", B:" + color.B);
			return b;
		}

		private void CreateLastUsedColorButtonRow(int x, int y, int w, int h)
		{
			for (int i = 0; i < 12; i++)
			{
				Button b = CreateColorButton(Color.Transparent, x, y, w, h);
				b.Enabled = false;
				_recentColorButtons.Add(b);
				x += w;
			}
			Controls.AddRange(_recentColorButtons.ToArray());
		}

		#endregion

		#region update user interface

		private void UpdateRecentColorsButtonRow()
		{
			if (RecentColors != null)
			{
				for (int i = 0; (i < RecentColors.Count) && (i < 12); i++)
				{
					_recentColorButtons[i].BackColor = RecentColors[i];
					_recentColorButtons[i].Enabled = true;
				}
			}
		}

		private void PreviewColor(Color colorToPreview, Control trigger)
		{
			_updateInProgress = true;
			colorPanel.BackColor = colorToPreview;
			if (trigger != textBoxHtmlColor)
			{
				textBoxHtmlColor.Text = ColorTranslator.ToHtml(colorToPreview);
			}
			if ((trigger != textBoxRed) && (trigger != textBoxGreen) && (trigger != textBoxBlue) && (trigger != textBoxAlpha))
			{
				textBoxRed.Text = colorToPreview.R.ToString();
				textBoxGreen.Text = colorToPreview.G.ToString();
				textBoxBlue.Text = colorToPreview.B.ToString();
				textBoxAlpha.Text = colorToPreview.A.ToString();
			}
			_updateInProgress = false;
		}

		private void AddToRecentColors(Color c)
		{
			if (RecentColors != null)
			{
				RecentColors.Remove(c);
				RecentColors.Insert(0, c);
				if (RecentColors.Count > 12)
				{
					RecentColors = RecentColors.Take(12).ToList();
				}
				UpdateRecentColorsButtonRow();
			}
		}

		#endregion

		#region textbox event handlers

		private void TextBoxHexadecimalTextChanged(object sender, EventArgs e)
		{
			if (_updateInProgress)
			{
				return;
			}
			TextBox textBox = (TextBox) sender;
			string text = textBox.Text.Replace("#", "");
			int i = 0;
			Color c;
			if (int.TryParse(text, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture, out i))
			{
				c = Color.FromArgb(i);
			}
			else
			{
				KnownColor knownColor;
				try
				{
					knownColor = (KnownColor) Enum.Parse(typeof(KnownColor), text, true);
					c = Color.FromKnownColor(knownColor);
				}
				catch (Exception)
				{
					return;
				}
			}
			Color opaqueColor = Color.FromArgb(255, c.R, c.G, c.B);
			PreviewColor(opaqueColor, textBox);
		}

		private void TextBoxRGBTextChanged(object sender, EventArgs e)
		{
			if (_updateInProgress)
			{
				return;
			}
			TextBox textBox = (TextBox) sender;
			PreviewColor(Color.FromArgb(GetColorPartIntFromString(textBoxAlpha.Text), GetColorPartIntFromString(textBoxRed.Text), GetColorPartIntFromString(textBoxGreen.Text), GetColorPartIntFromString(textBoxBlue.Text)), textBox);
		}

		private void TextBoxGotFocus(object sender, EventArgs e)
		{
			textBoxHtmlColor.SelectAll();
		}

		private void TextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.KeyCode == Keys.Return) || (e.KeyCode == Keys.Enter))
			{
				AddToRecentColors(colorPanel.BackColor);
			}
		}

		#endregion

		#region button event handlers

		private void ColorButtonClick(object sender, EventArgs e)
		{
			Button b = (Button) sender;
			PreviewColor(b.BackColor, b);
		}

		private void BtnTransparentClick(object sender, EventArgs e)
		{
			ColorButtonClick(sender, e);
		}

		private void BtnApplyClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Hide();
			AddToRecentColors(colorPanel.BackColor);
		}

		#endregion
	}
}