using PowerProfile.App.Models;
using PowerProfile.App.Native;
using PowerProfile.App.Services;

namespace PowerProfile.App.UI;

/// <summary>
/// Main application window.  Two tab pages:
///   1. Refresh Rate  – discover rates, set AC/Battery targets, apply now
///   2. Animations    – master toggle + AC/Battery sub-options
/// A status bar shows the current power source and active settings.
/// </summary>
public sealed class MainForm : Form
{
    // ── State ────────────────────────────────────────────────────────────────

    private readonly SettingsManager _mgr = new();
    private List<uint> _rates = [];

    // ── Controls – layout ────────────────────────────────────────────────────

    private TabControl   _tabs         = null!;
    private TabPage      _tabRefresh   = null!;
    private TabPage      _tabAnim      = null!;
    private StatusStrip  _status       = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private Button       _btnApplyNow  = null!;
    private NotifyIcon    _trayIcon    = null!;
    private ContextMenuStrip _trayMenu = null!;
    private bool          _allowClose;
    private bool          _trayDisposed;

    // Refresh-rate tab
    private CheckBox     _chkRrEnabled     = null!;
    private CheckBox     _chkRrAuto        = null!;
    private Label        _lblRrAC          = null!;
    private ComboBox     _cmbRrAC          = null!;
    private Label        _lblRrBattery     = null!;
    private ComboBox     _cmbRrBattery     = null!;
    private Label        _lblCurrentMode   = null!;


    // Animation tab
    private CheckBox     _chkAniEnabled    = null!;
    private CheckBox     _chkAniAuto       = null!;
    private CheckBox     _chkAniOnAC       = null!;
    private CheckBox     _chkAniOnBattery  = null!;

    private Label        _lblAniCurrent    = null!;

    // ── Constructor ──────────────────────────────────────────────────────────

    public MainForm()
    {
        _mgr.Load();
        BuildUI();
        BuildTrayIcon();
        PopulateRefreshRates();
        LoadSettingsIntoUI();
        RefreshStatusBar();
    }

    // ── UI construction ──────────────────────────────────────────────────────

    private void BuildUI()
    {
        Text = "PowerProfile";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimumSize = new Size(560, 440);
        ClientSize = new Size(620, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(243, 245, 247);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            Padding = new Padding(24, 16, 24, 18),
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var title = new Label
        {
            Text = "PowerProfile",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 15f),
            ForeColor = Color.FromArgb(30, 34, 40),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0),
        };
        var subtitle = new Label
        {
            Text = "Manage display refresh rate and Windows animations",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(95, 102, 112),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0),
        };
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(subtitle, 0, 1);

        _status = new StatusStrip
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 32,
            SizingGrip = false,
            Margin = new Padding(0),
        };
        _lblStatus = new ToolStripStatusLabel("Ready")
        {
            AutoSize = false,
            Height = 28,
            Padding = new Padding(8, 2, 8, 2),
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _status.Items.Add(_lblStatus);

        _btnApplyNow = new Button
        {
            Text = "Apply changes",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(35, 112, 210),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10f),
            Margin = new Padding(24, 8, 24, 10),
        };
        _btnApplyNow.FlatAppearance.BorderSize = 0;
        _btnApplyNow.Click += BtnApplyNow_Click;

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(18, 8),
            Margin = new Padding(0),
        };
        _tabRefresh = new TabPage("Refresh Rate");
        _tabAnim = new TabPage("Animations");
        BuildRefreshTab();
        BuildAnimationTab();
        _tabs.TabPages.Add(_tabRefresh);
        _tabs.TabPages.Add(_tabAnim);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_tabs, 0, 1);
        root.Controls.Add(_btnApplyNow, 0, 2);
        root.Controls.Add(_status, 0, 3);
        Controls.Add(root);
    }

    private void BuildTrayIcon()
    {
        Icon = CreateAppIcon();

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Open PowerProfile", null, (_, _) => OpenFlutterUi());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("Exit PowerProfile", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Icon = (Icon)Icon.Clone(),
            Text = "PowerProfile",
            ContextMenuStrip = _trayMenu,
            Visible = true,
        };
        // Keep the native host available for backend/tray services while the
        // Flutter dashboard is the primary visible application surface.
        _trayIcon.MouseUp += TrayIcon_MouseUp;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
    }

    private void TrayIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            OpenFlutterUi();
    }

    private void OpenFlutterUi()
    {
        if (FlutterUiLauncher.Launch())
        {
            Hide();
            return;
        }

        Show();
        MessageBox.Show(this,
            "The Flutter dashboard executable was not found. Build PowerProfile.FlutterUI for development or reinstall PowerProfile.",
            "PowerProfile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    internal bool LaunchPrimaryUi() => FlutterUiLauncher.Launch();

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose) return;

        e.Cancel = true;
        Hide();
        _trayIcon.ShowBalloonTip(1200, "PowerProfile", "PowerProfile is still running in the notification area.", ToolTipIcon.Info);
    }

    private void ExitApplication()
    {
        _allowClose = true;
        Close();
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_trayDisposed)
            return;

        _trayDisposed = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
    }

    private static Icon CreateAppIcon()
    {
        // ApplicationIcon embeds assets/PowerProfile.ico in the EXE. Extracting the
        // associated icon avoids a file-path dependency and cloning prevents the
        // NotifyIcon and Form from sharing a disposable native handle.
        using var associated = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return associated is null
            ? SystemIcons.Application
            : (Icon)associated.Clone();
    }

    private void BuildRefreshTab()
    {
        _tabRefresh.Padding = new Padding(18);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            AutoSize = true,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _chkRrEnabled = Chk("Enable refresh-rate control", 0, 0);
        _chkRrAuto    = Chk("Auto-switch based on power source", 0, 0);
        _lblCurrentMode = Lbl("Current mode: —", 0, 0);
        _lblRrAC    = Lbl("AC power rate:", 0, 0);
        _cmbRrAC    = Cmb(0, 0, 0);
        _lblRrBattery = Lbl("Battery rate:", 0, 0);
        _cmbRrBattery = Cmb(0, 0, 0);
        _chkRrEnabled.CheckedChanged += (_, _) => SyncRrEnabled();
        _chkRrAuto.CheckedChanged    += (_, _) => SyncRrEnabled();

        _chkRrEnabled.AutoSize = true;
        _chkRrEnabled.Margin = new Padding(0, 0, 0, 6);
        _chkRrAuto.AutoSize = true;
        _chkRrAuto.Margin = new Padding(0, 0, 0, 10);
        _lblCurrentMode.AutoSize = true;
        _lblCurrentMode.Margin = new Padding(0, 0, 0, 14);

        _lblRrAC.Dock = DockStyle.Fill;
        _lblRrAC.TextAlign = ContentAlignment.MiddleLeft;
        _lblRrAC.Margin = new Padding(0, 0, 14, 12);
        _cmbRrAC.Dock = DockStyle.Fill;
        _cmbRrAC.Margin = new Padding(0, 0, 0, 12);

        _lblRrBattery.Dock = DockStyle.Fill;
        _lblRrBattery.TextAlign = ContentAlignment.MiddleLeft;
        _lblRrBattery.Margin = new Padding(0, 0, 14, 14);
        _cmbRrBattery.Dock = DockStyle.Fill;
        _cmbRrBattery.Margin = new Padding(0, 0, 0, 14);

        var rrGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 4, 0, 0),
        };
        rrGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        rrGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rrGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rrGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rrGrid.Controls.Add(_lblRrAC, 0, 0);
        rrGrid.Controls.Add(_cmbRrAC, 1, 0);
        rrGrid.Controls.Add(_lblRrBattery, 0, 1);
        rrGrid.Controls.Add(_cmbRrBattery, 1, 1);

        table.Controls.Add(_chkRrEnabled, 0, 0);
        table.SetColumnSpan(_chkRrEnabled, 2);
        table.Controls.Add(_chkRrAuto, 0, 1);
        table.SetColumnSpan(_chkRrAuto, 2);
        table.Controls.Add(_lblCurrentMode, 0, 2);
        table.SetColumnSpan(_lblCurrentMode, 2);
        table.Controls.Add(rrGrid, 0, 3);
        table.SetColumnSpan(rrGrid, 2);
        card.Controls.Add(table);
        _tabRefresh.Controls.Add(card);
    }

    private void BuildAnimationTab()
    {
        _tabAnim.Padding = new Padding(18);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            AutoSize = true,
        };
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _chkAniEnabled   = Chk("Enable animation control", 0, 0);
        _chkAniAuto      = Chk("Auto-switch based on power source", 0, 0);
        _chkAniOnAC      = Chk("Animations ON when on AC power", 0, 0);
        _chkAniOnBattery = Chk("Animations ON when on battery", 0, 0);
        _lblAniCurrent   = Lbl("Current state: —", 0, 0);
        _chkAniEnabled.CheckedChanged += (_, _) => SyncAniEnabled();
        _chkAniAuto.CheckedChanged    += (_, _) => SyncAniEnabled();

        foreach (var chk in new[] { _chkAniEnabled, _chkAniAuto, _chkAniOnAC, _chkAniOnBattery })
        {
            chk.AutoSize = true;
            chk.Margin = new Padding(0, 0, 0, 8);
        }

        _lblAniCurrent.AutoSize = true;
        _lblAniCurrent.Margin = new Padding(0, 4, 0, 14);

        table.Controls.Add(_chkAniEnabled, 0, 0);
        table.Controls.Add(_chkAniAuto, 0, 1);
        table.Controls.Add(_chkAniOnAC, 0, 2);
        table.Controls.Add(_chkAniOnBattery, 0, 3);
        table.Controls.Add(_lblAniCurrent, 0, 4);
        card.Controls.Add(table);
        _tabAnim.Controls.Add(card);
    }

    // ── Helpers: control factory ─────────────────────────────────────────────

    private static CheckBox Chk(string text, int x, int y) =>
        new() { Text = text, Left = x, Top = y, AutoSize = true };

    private static Label Lbl(string text, int x, int y) =>
        new() { Text = text, Left = x, Top = y, AutoSize = true };

    private static ComboBox Cmb(int x, int y, int width) =>
        new() { Left = x, Top = y, Width = width, DropDownStyle = ComboBoxStyle.DropDownList };

    // ── Populate / sync ──────────────────────────────────────────────────────

    private void PopulateRefreshRates()
    {
        try
        {
            _rates = DisplayService.GetAvailableRefreshRates();
            var current = DisplayService.GetCurrentMode();
            _lblCurrentMode.Text = $"Current mode: {current}";

            foreach (var cmb in new[] { _cmbRrAC, _cmbRrBattery })
            {
                cmb.Items.Clear();
                cmb.Items.Add("Auto (Highest)");
                cmb.Items.Add("Auto (Lowest)");
                foreach (uint r in _rates)
                    cmb.Items.Add($"{r} Hz");
                cmb.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _lblCurrentMode.Text = $"Detection failed: {ex.Message}";
        }
    }

    private void LoadSettingsIntoUI()
    {
        var s = _mgr.Settings;

        _chkRrEnabled.Checked    = s.RefreshRateEnabled;
        _chkRrAuto.Checked       = s.RefreshRateAutoSwitch;
        SetComboToRate(_cmbRrAC,      s.RefreshRateAC);
        SetComboToRate(_cmbRrBattery, s.RefreshRateBattery);

        _chkAniEnabled.Checked    = s.AnimationsEnabled;
        _chkAniAuto.Checked       = s.AnimationsAutoSwitch;
        _chkAniOnAC.Checked       = s.AnimationsOnAC;
        _chkAniOnBattery.Checked  = s.AnimationsOnBattery;

        SyncRrEnabled();
        SyncAniEnabled();
        UpdateAniCurrentLabel();
    }

    private void SetComboToRate(ComboBox cmb, uint rate)
    {
        if (rate == 0) { cmb.SelectedIndex = 0; return; }
        for (int i = 0; i < cmb.Items.Count; i++)
            if (cmb.Items[i] is string s && s == $"{rate} Hz")
            { cmb.SelectedIndex = i; return; }
        cmb.SelectedIndex = 0;
    }

    private uint GetComboRate(ComboBox cmb, bool highWhenZero)
    {
        if (cmb.SelectedIndex <= 1) return 0; // "Highest" or "Lowest" → auto
        if (cmb.SelectedItem is string s && s.EndsWith(" Hz") &&
            uint.TryParse(s.Replace(" Hz", ""), out uint r)) return r;
        return 0;
    }

    private void SyncRrEnabled()
    {
        bool master = _chkRrEnabled.Checked;
        bool auto   = _chkRrAuto.Checked;
        _chkRrAuto.Enabled    = master;
        _lblRrAC.Enabled      = master;
        _cmbRrAC.Enabled      = master;
        _lblRrBattery.Enabled = master && auto;
        _cmbRrBattery.Enabled = master && auto;

    }

    private void SyncAniEnabled()
    {
        bool master = _chkAniEnabled.Checked;
        bool auto   = _chkAniAuto.Checked;
        _chkAniAuto.Enabled      = master;
        _chkAniOnAC.Enabled      = master && auto;
        _chkAniOnBattery.Enabled = master && auto;

    }

    private void UpdateAniCurrentLabel()
    {
        try
        {
            bool on = AnimationService.GetMinAnimateEnabled();
            _lblAniCurrent.Text = $"Current state: animations {(on ? "ON" : "OFF")}";
        }
        catch { _lblAniCurrent.Text = "Current state: unknown"; }
    }

    private void RefreshStatusBar()
    {
        var src = PowerStateService.GetCurrentSource();
        string srcLabel = src switch
        {
            PowerStateService.PowerSource.AC      => "AC power",
            PowerStateService.PowerSource.Battery => "Battery",
            _                                     => "Unknown source",
        };
        try
        {
            var mode = DisplayService.GetCurrentMode();
            _lblStatus.Text = $"{srcLabel}  |  {mode}  |  Animations: {(AnimationService.GetMinAnimateEnabled() ? "ON" : "OFF")}";
        }
        catch
        {
            _lblStatus.Text = srcLabel;
        }
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void ApplyRefreshRate()
    {
        if (!_chkRrEnabled.Checked) return;

        bool onAC  = PowerStateService.IsOnAC();
        var  cmb   = (_chkRrAuto.Checked && !onAC) ? _cmbRrBattery : _cmbRrAC;
        uint targetHz = ResolveRate(cmb, pickHighest: onAC);

        if (targetHz == 0) { SetStatus("No valid rate selected."); return; }

        bool ok = DisplayService.SetRefreshRate(targetHz);
        SetStatus(ok
            ? $"Refresh rate set to {targetHz} Hz."
            : $"Failed to set {targetHz} Hz (may need admin rights or unsupported rate).");
        RefreshStatusBar();
        _lblCurrentMode.Text = $"Current mode: {DisplayService.GetCurrentMode()}";
        SaveSettings();
    }

    private void ApplyAnimations()
    {
        if (!_chkAniEnabled.Checked) return;

        bool enable;
        if (_chkAniAuto.Checked)
            enable = PowerStateService.IsOnAC() ? _chkAniOnAC.Checked : _chkAniOnBattery.Checked;
        else
            enable = _chkAniOnAC.Checked;

        AnimationService.SetAnimations(enable);
        SetStatus($"Animations {(enable ? "enabled" : "disabled")}.");
        RefreshStatusBar();
        UpdateAniCurrentLabel();
        SaveSettings();
    }

    private void BtnApplyNow_Click(object? sender, EventArgs e)
    {
        ApplyRefreshRate();
        ApplyAnimations();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private uint ResolveRate(ComboBox cmb, bool pickHighest)
    {
        if (cmb.SelectedIndex == 0) // "Highest"
            return _rates.Count > 0 ? _rates[^1] : 0;
        if (cmb.SelectedIndex == 1) // "Lowest"
            return _rates.Count > 0 ? _rates[0] : 0;
        return GetComboRate(cmb, pickHighest);
    }

    private void SetStatus(string msg) => _lblStatus.Text = msg;

    private void SaveSettings()
    {
        var s = _mgr.Settings;
        s.RefreshRateEnabled   = _chkRrEnabled.Checked;
        s.RefreshRateAutoSwitch= _chkRrAuto.Checked;
        s.RefreshRateAC        = GetComboRate(_cmbRrAC, true);
        s.RefreshRateBattery   = GetComboRate(_cmbRrBattery, false);
        s.AnimationsEnabled    = _chkAniEnabled.Checked;
        s.AnimationsAutoSwitch = _chkAniAuto.Checked;
        s.AnimationsOnAC       = _chkAniOnAC.Checked;
        s.AnimationsOnBattery  = _chkAniOnBattery.Checked;
        _mgr.Save();
    }
}
