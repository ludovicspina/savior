using System;
using System.Drawing;
using System.Windows.Forms;

namespace Savior.UI
{
    public class DarkTabControl : TabControl
    {
        public DarkTabControl()
        {
            // Take full control of painting
            SetStyle(ControlStyles.UserPaint | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer, true);
            
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(120, 30); // Fixed size for better look
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var headerColor = Color.FromArgb(30, 30, 30); // Darker background for header
            var activeColor = Color.FromArgb(45, 45, 48); // Matches Form background
            var textColor = Color.White;

            // 1. Fill the entire control background (this covers the empty space behind tabs)
            g.Clear(headerColor);

            // 2. Draw Tabs
            for (int i = 0; i < TabCount; i++)
            {
                var tabRect = GetTabRect(i);
                bool selected = (SelectedIndex == i);

                // Tab Background
                using (var brush = new SolidBrush(selected ? activeColor : headerColor))
                {
                    g.FillRectangle(brush, tabRect);
                }

                // Tab Text
                TextRenderer.DrawText(g, TabPages[i].Text, Font, tabRect, textColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                
                // Optional: Selection Indicator (e.g., a colored line at the bottom of the active tab)
                if (selected)
                {
                    using (var pen = new Pen(Color.Cyan, 2))
                    {
                        g.DrawLine(pen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
                    }
                }
            }

            // 3. Draw a border/line separating header from content if needed
            // For a flat look, we might just want the active tab to blend in.
            // Since activeColor matches the page background, it should blend perfectly.
        }
    }
}
