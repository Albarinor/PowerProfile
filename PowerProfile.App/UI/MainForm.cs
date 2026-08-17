using PowerProfile.App.Models;
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

    // Refresh-rate tab
    private CheckBox     _chkRrEnabled     = null!;
    private CheckBox     _chkRrAuto        = null!;
    private Label        _lblRrAC          = null!;
    private ComboBox     _cmbRrAC          = null!;
    private Label        _lblRrBattery     = null!;
    private ComboBox     _cmbRrBattery     = null!;
    private Label        _lblCurrentMode   = null!;
    private Button       _btnRrApply       = null!;

    // Animation tab
    private CheckBox     _chkAniEnabled    = null!;
    private CheckBox     _chkAniAuto       = null!;
    private CheckBox     _chkAniOnAC       = null!;
    private CheckBox     _chkAniOnBattery  = null!;
    private Button       _btnAniApply      = null!;
    private Label        _lblAniCurrent    = null!;

    // ── Constructor ──────────────────────────────────────────────────────────

    public MainForm()
    {
        _mgr.Load();
        BuildUI();
        PopulateRefreshRates();
        LoadSettingsIntoUI();
        RefreshStatusBar();
    }

    // ── UI construction ──────────────────────────────────────────────────────

    private void BuildUI()
    {
        Text            = "PowerProfile";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Size            = new Size(480, 400);
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("Segoe UI", 9f);

        // Status bar
        _status    = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _status.Items.Add(_lblStatus);

        // Apply Now button
        _btnApplyNow = new Button
        {
            Text     = "Apply Now (auto-detect power source)",
            Dock     = DockStyle.Bottom,
            Height   = 32,
        };
        _btnApplyNow.Click += BtnApplyNow_Click;

        // Tabs
        _tabs       = new TabControl { Dock = DockStyle.Fill };
        _tabRefresh = new TabPage("Refresh Rate");
        _tabAnim    = new TabPage("Animations");
        BuildRefreshTab();
        BuildAnimationTab();
        _tabs.TabPages.Add(_tabRefresh);
        _tabs.TabPages.Add(_tabAnim);

        Controls.Add(_tabs);
        Controls.Add(_btnApplyNow);
        Controls.Add(_status);
    }

    private void BuildRefreshTab()
    {
        int y = 12;

        _chkRrEnabled = Chk("Enable refresh-rate control", 12, y); y += 28;
        _chkRrAuto    = Chk("Auto-switch based on power source", 24, y); y += 28;

        _lblCurrentMode = Lbl("Current mode: —", 12, y); y += 22;

        _lblRrAC    = Lbl("AC power rate:", 12, y);
        _cmbRrAC    = Cmb(140, y, 90); y += 30;

        _lblRrBattery = Lbl("Battery rate:", 12, y);
        _cmbRrBattery = Cmb(140, y, 90); y += 30;

        _btnRrApply = new Button { Text = "Apply Refresh Rate", Left = 12, Top = y, Width = 160, Height = 28 };
        _btnRrApply.Click += BtnRrApply_Click;

        _chkRrEnabled.CheckedChanged += (_, _) => SyncRrEnabled();
        _chkRrAuto.CheckedChanged    += (_, _) => SyncRrEnabled();

        foreach (Control c in new Control[]
            { _chkRrEnabled, _chkRrAuto, _lblCurrentMode,
              _lblRrAC, _cmbRrAC, _lblRrBattery, _cmbRrBattery, _btnRrApply })
            _tabRefresh.Controls.Add(c);
    }

    private void BuildAnimationTab()
    {
        int y = 12;

        _chkAniEnabled   = Chk("Enable animation control",              12, y); y += 28;
        _chkAniAuto      = Chk("Auto-switch based on power source",      24, y); y += 28;
        _chkAniOnAC      = Chk("Animations ON when on AC power",         36, y); y += 24;
        _chkAniOnBattery = Chk("Animations ON when on battery",          36, y); y += 32;

        _lblAniCurrent = Lbl("Current state: —", 12, y); y += 22;

        _btnAniApply = new Button { Text = "Apply Animations", Left = 12, Top = y, Width = 150, Height = 28 };
        _btnAniApply.Click += BtnAniApply_Click;

        _chkAniEnabled.CheckedChanged += (_, _) => SyncAniEnabled();
        _chkAniAuto.CheckedChanged    += (_, _) => SyncAniEnabled();

        foreach (Control c in new Control[]
            { _chkAniEnabled, _chkAniAuto, _chkAniOnAC, _chkAniOnBattery,
              _lblAniCurrent, _btnAniApply })
            _tabAnim.Controls.Add(c);
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
                cmb.Items.Add("(Highest)");
                cmb.Items.Add("(Lowest)");
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
        _btnRrApply.Enabled   = master;
    }

    private void SyncAniEnabled()
    {
        bool master = _chkAniEnabled.Checked;
        bool auto   = _chkAniAuto.Checked;
        _chkAniAuto.Enabled      = master;
        _chkAniOnAC.Enabled      = master && auto;
        _chkAniOnBattery.Enabled = master && auto;
        _btnAniApply.Enabled     = master;
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

    private void BtnRrApply_Click(object? sender, EventArgs e)
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

    private void BtnAniApply_Click(object? sender, EventArgs e)
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
        BtnRrApply_Click(sender, e);
        BtnAniApply_Click(sender, e);
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
