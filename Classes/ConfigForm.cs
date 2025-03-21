using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RichPresenceApp.Classes
{
    public class ConfigForm : Form
    {
        // CS2 Theme Colors
        private static readonly Color BackgroundColor = Color.FromArgb(24, 26, 27);
        private static readonly Color PanelColor = Color.FromArgb(32, 34, 37);
        private static readonly Color AccentColor = Color.FromArgb(255, 165, 0); // CS2 Orange
        private static readonly Color TextColor = Color.FromArgb(220, 221, 222);
        private static readonly Color SecondaryTextColor = Color.FromArgb(142, 146, 151);

        // Controls - initialize with null to make them nullable
        private Panel? _mainPanel;
        private Label? _titleLabel;
        private Label? _displaySettingsLabel;
        private Label? _appSettingsLabel;
        private CheckBox? _showMapCheckBox;
        private CheckBox? _showGameModeCheckBox;
        private CheckBox? _showScoreCheckBox;
        private CheckBox? _showTeamCheckBox;
        private CheckBox? _minimizeToTrayCheckBox;
        private CheckBox? _startWithWindowsCheckBox;
        private CS2Button? _saveButton;
        private CS2Button? _cancelButton;

        // Constructor
        public ConfigForm()
        {
            // Initialize form
            InitializeComponent();

            // Load settings
            LoadSettings();

            // Apply custom styling to checkboxes
            ApplyCS2StyleToCheckBoxes();
        }

        // Initialize component
        private void InitializeComponent()
        {
            // Set form properties
            Text = "CS2 Rich Presence Settings";
            Size = new Size(450, 500); // Increased height for new controls
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BackgroundColor;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // Create main panel
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BackgroundColor,
                Padding = new Padding(20)
            };

            // Create title label
            _titleLabel = new Label
            {
                Text = "CS2 Rich Presence Settings",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = AccentColor,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Create section labels
            _displaySettingsLabel = new Label
            {
                Text = "Display Settings",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(30, 70)
            };

            _appSettingsLabel = new Label
            {
                Text = "Application Settings",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(30, 240)
            };

            // Create display settings checkboxes
            _showMapCheckBox = new CheckBox
            {
                Text = "Show Map",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(40, 105),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };

            _showGameModeCheckBox = new CheckBox
            {
                Text = "Show Game Mode",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(40, 140),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };

            _showScoreCheckBox = new CheckBox
            {
                Text = "Show Score",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(40, 175),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };

            _showTeamCheckBox = new CheckBox
            {
                Text = "Show Team",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(40, 210),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };

            // Create application settings checkboxes
            _minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to Tray",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(40, 275),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };

            _startWithWindowsCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                ForeColor = TextColor,
                AutoSize = true,
                Location = new Point(40, 310),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };

            // Create buttons
            _saveButton = new CS2Button
            {
                Text = "SAVE",
                Location = new Point(230, 400),
                Size = new Size(90, 35),
                BackColor = AccentColor,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };

            _cancelButton = new CS2Button
            {
                Text = "CANCEL",
                Location = new Point(330, 400),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };

            // Add event handlers
            if (_saveButton != null) _saveButton.Click += SaveButton_Click;
            if (_cancelButton != null) _cancelButton.Click += CancelButton_Click;

            // Add controls to form
            if (_mainPanel != null)
            {
                if (_titleLabel != null) _mainPanel.Controls.Add(_titleLabel);
                if (_displaySettingsLabel != null) _mainPanel.Controls.Add(_displaySettingsLabel);
                if (_appSettingsLabel != null) _mainPanel.Controls.Add(_appSettingsLabel);
                if (_showMapCheckBox != null) _mainPanel.Controls.Add(_showMapCheckBox);
                if (_showGameModeCheckBox != null) _mainPanel.Controls.Add(_showGameModeCheckBox);
                if (_showScoreCheckBox != null) _mainPanel.Controls.Add(_showScoreCheckBox);
                if (_showTeamCheckBox != null) _mainPanel.Controls.Add(_showTeamCheckBox);
                if (_minimizeToTrayCheckBox != null) _mainPanel.Controls.Add(_minimizeToTrayCheckBox);
                if (_startWithWindowsCheckBox != null) _mainPanel.Controls.Add(_startWithWindowsCheckBox);
                if (_saveButton != null) _mainPanel.Controls.Add(_saveButton);
                if (_cancelButton != null) _mainPanel.Controls.Add(_cancelButton);

                Controls.Add(_mainPanel);
            }
        }

        // Apply CS2 style to checkboxes
        private void ApplyCS2StyleToCheckBoxes()
        {
            if (_mainPanel == null) return;

            foreach (Control control in _mainPanel.Controls)
            {
                if (control is CheckBox checkBox)
                {
                    checkBox.FlatStyle = FlatStyle.Flat;
                    checkBox.FlatAppearance.BorderSize = 0;
                    checkBox.FlatAppearance.CheckedBackColor = Color.Transparent;
                    checkBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 43, 48);
                    checkBox.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 53, 58);
                    checkBox.Padding = new Padding(5, 0, 0, 0);
                }
            }
        }

        // Load settings
        private void LoadSettings()
        {
            try
            {
                if (Config.Current == null)
                {
                    ConsoleManager.WriteLine("Configuration not loaded, cannot load settings", ConsoleColor.Red, true);
                    return;
                }

                // Set display settings checkbox values
                if (_showMapCheckBox != null) _showMapCheckBox.Checked = Config.Current.ShowMap;
                if (_showGameModeCheckBox != null) _showGameModeCheckBox.Checked = Config.Current.ShowGameMode;
                if (_showScoreCheckBox != null) _showScoreCheckBox.Checked = Config.Current.ShowScore;
                if (_showTeamCheckBox != null) _showTeamCheckBox.Checked = Config.Current.ShowTeam;

                // Set application settings checkbox values
                if (_minimizeToTrayCheckBox != null) _minimizeToTrayCheckBox.Checked = Config.Current.MinimizeToTray;
                if (_startWithWindowsCheckBox != null) _startWithWindowsCheckBox.Checked = Config.Current.StartWithWindows;

                // Check if startup registry key exists
                bool startupExists = IsStartupEnabled();
                if (_startWithWindowsCheckBox != null && startupExists != Config.Current.StartWithWindows)
                {
                    // Update checkbox to match actual registry state
                    _startWithWindowsCheckBox.Checked = startupExists;
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error loading settings: {ex.Message}", ConsoleColor.Red, true);
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Save button click
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (Config.Current == null)
                {
                    ConsoleManager.WriteLine("Configuration not loaded, cannot save settings", ConsoleColor.Red, true);
                    return;
                }

                // Update display settings
                if (_showMapCheckBox != null) Config.Current.ShowMap = _showMapCheckBox.Checked;
                if (_showGameModeCheckBox != null) Config.Current.ShowGameMode = _showGameModeCheckBox.Checked;
                if (_showScoreCheckBox != null) Config.Current.ShowScore = _showScoreCheckBox.Checked;
                if (_showTeamCheckBox != null) Config.Current.ShowTeam = _showTeamCheckBox.Checked;

                // Update application settings
                if (_minimizeToTrayCheckBox != null) Config.Current.MinimizeToTray = _minimizeToTrayCheckBox.Checked;
                if (_startWithWindowsCheckBox != null)
                {
                    bool oldValue = Config.Current.StartWithWindows;
                    bool newValue = _startWithWindowsCheckBox.Checked;

                    // Update config
                    Config.Current.StartWithWindows = newValue;

                    // Update startup registry
                    if (oldValue != newValue)
                    {
                        if (newValue)
                        {
                            EnableStartup();
                        }
                        else
                        {
                            DisableStartup();
                        }
                    }
                }

                // Save config
                Config.Save();

                // Close form
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error saving settings: {ex.Message}", ConsoleColor.Red, true);
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cancel button click
        private void CancelButton_Click(object? sender, EventArgs e)
        {
            // Close form
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // Check if startup is enabled
        private bool IsStartupEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
                return key?.GetValue(Program.AppName) != null;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error checking startup registry: {ex.Message}", ConsoleColor.Red, true);
                return false;
            }
        }

        // Enable startup
        private void EnableStartup()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key != null)
                {
                    string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    key.SetValue(Program.AppName, $"\"{appPath}\"");
                    ConsoleManager.WriteLine("Added application to startup", ConsoleColor.Green, true);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error adding to startup: {ex.Message}", ConsoleColor.Red, true);
                throw;
            }
        }

        // Disable startup
        private void DisableStartup()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key != null)
                {
                    key.DeleteValue(Program.AppName, false);
                    ConsoleManager.WriteLine("Removed application from startup", ConsoleColor.Green, true);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error removing from startup: {ex.Message}", ConsoleColor.Red, true);
                throw;
            }
        }

        // Override OnPaint to draw a border
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw border
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                AccentColor, 1, ButtonBorderStyle.Solid,
                AccentColor, 1, ButtonBorderStyle.Solid,
                AccentColor, 1, ButtonBorderStyle.Solid,
                AccentColor, 1, ButtonBorderStyle.Solid);
        }
    }

    // Custom button class for CS2 style
    public class CS2Button : Button
    {
        private bool _isHovering = false;
        private bool _isPressed = false;

        public CS2Button()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Calculate colors based on state
            Color buttonColor = BackColor;
            if (_isPressed)
            {
                // Darken when pressed
                buttonColor = ControlPaint.Dark(BackColor, 0.1f);
            }
            else if (_isHovering)
            {
                // Lighten when hovering
                buttonColor = ControlPaint.Light(BackColor, 0.1f);
            }

            // Draw button background
            using (SolidBrush brush = new SolidBrush(buttonColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Draw text
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}