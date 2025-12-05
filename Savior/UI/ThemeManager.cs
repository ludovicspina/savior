using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Savior.UI
{
    public static class ThemeManager
    {
        // DWM Window Attributes
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_MICA_EFFECT = 1029;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // Backdrop Types
        private const int DWMSBT_AUTO = 0;
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_MAINWINDOW = 2; // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4; // Tabbed

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public static Color BackColor { get; } = Color.FromArgb(32, 32, 32);
        public static Color ForeColor { get; } = Color.White;
        public static Color ControlBackColor { get; } = Color.FromArgb(45, 45, 45);
        public static Color AccentColor { get; } = Color.FromArgb(0, 120, 212); // Windows Blue

        public static void ApplyTheme(Form form)
        {
            // 1. Activer le Dark Mode pour la barre de titre
            int useDarkMode = 1;
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

            // 2. Tenter d'activer Mica (Windows 11 build 22621+)
            if (IsWindows11())
            {
                int backdropType = DWMSBT_MAINWINDOW; // Mica
                DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                
                // Pour Mica, le fond doit être transparent
                form.BackColor = Color.Black; 
            }
            else
            {
                // Fallback pour Windows 10
                form.BackColor = BackColor;
            }

            form.ForeColor = ForeColor;
            form.Padding = new Padding(0); // Supprimer le padding pour éviter la barre noire en bas

            // Appliquer le thème aux contrôles enfants récursivement
            ApplyThemeToControls(form.Controls);
        }

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                UpdateControlTheme(c);
                if (c.HasChildren)
                {
                    ApplyThemeToControls(c.Controls);
                }
            }
        }

        private static void UpdateControlTheme(Control c)
        {
            // Appliquer le thème sombre aux ScrollBars natifs (ListView, TreeView, etc.)
            if (c is ListView || c is TreeView || c is ListBox || c is ScrollableControl)
            {
                SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
            }

            // Ne pas toucher aux contrôles custom qui gèrent leur propre thème
            if (c is ModernButton || c is DarkTabControl) return;

            if (c is Label || c is CheckBox || c is RadioButton || c is GroupBox)
            {
                c.ForeColor = ForeColor;
                c.BackColor = Color.Transparent; // Important pour Mica
            }
            else if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel)
            {
                c.BackColor = Color.Transparent; // Laisser passer Mica
                c.ForeColor = ForeColor;
            }
            else if (c is TextBox || c is ListBox || c is ComboBox)
            {
                c.BackColor = ControlBackColor;
                c.ForeColor = ForeColor;
            }
            else if (c is TabControl tc)
            {
                // Forcer le TabControl à remplir toute la fenêtre pour éviter la barre noire en bas
                tc.Dock = DockStyle.Fill;
                tc.ForeColor = ForeColor;
            }
            else if (c is Button btn)
            {
                // Fallback pour les boutons standard non migrés vers ModernButton
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
                btn.BackColor = Color.FromArgb(25, 255, 255, 255); // Semi-transparent
                btn.ForeColor = ForeColor;
                btn.Cursor = Cursors.Hand;
            }
        }

        private static bool IsWindows11()
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;
        }
    }
}
