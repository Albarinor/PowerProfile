import 'dart:io';
import 'package:flutter/material.dart';
import 'power_profile_service.dart';

void main() => runApp(
  PowerProfileApp(
    service: Platform.isWindows
        ? WindowsPowerProfileService()
        : MockPowerProfileService(),
  ),
);

enum PageSection { overview, refresh, animations }

enum AppLanguage { zh, en }

class Texts {
  const Texts(this.lang);
  final AppLanguage lang;
  static const _en = {
    'overview': 'Overview',
    'refresh': 'Refresh rate',
    'animations': 'Animations',
    'dashboard': 'Power dashboard',
    'subtitle': 'Display and Windows animation controls',
    'language': 'Language',
    'english': 'English',
    'chinese': '中文',
    'apply': 'Apply changes',
    'applying': 'Applying...',
    'enableRefresh': 'Enable refresh-rate control',
    'powerPolicy': 'Auto-switch by power source',
    'acRate': 'AC target rate',
    'batteryRate': 'Battery target rate',
    'enableAnimations': 'Enable animation control',
    'acPolicy': 'Animations on AC',
    'batteryPolicy': 'Animations on battery',
    'currentDisplay': 'CURRENT DISPLAY',
    'unavailable': 'Refresh rate unavailable',
    'noRates': 'No refresh rates reported',
    'selectRate': 'Select a rate',
    'connected': 'Native service connected',
    'connecting': 'Connecting to native service...',
    'applyingStatus': 'Applying changes through the native service...',
    'applied': 'Changes applied',
    'failed': 'Changes were not applied',
    'nativeUnavailable': 'Native service unavailable',
    'retry': 'Retry connection',
    'window': 'Minimize/maximize windows',
    'taskbar': 'Taskbar and thumbnail preview animations',
    'menu': 'Menu animation',
    'combo': 'Combo-box animation',
    'list': 'List-box smooth scrolling',
    'selection': 'Selection fade',
    'tooltip': 'Tooltip animation',
    'tooltipFade': 'Tooltip fade',
    'cursor': 'Cursor shadow',
    'ui': 'UI effects',
    'client': 'Client-area animation',
    'overlap': 'Disable overlapped content',
  };
  static const _zh = {
    'overview': '概览',
    'refresh': '刷新率',
    'animations': '动画效果',
    'dashboard': '电源控制面板',
    'subtitle': '显示器与 Windows 动画控制',
    'language': '语言',
    'english': 'English',
    'chinese': '中文',
    'apply': '应用更改',
    'applying': '正在应用...',
    'enableRefresh': '启用刷新率控制',
    'powerPolicy': '按电源使用独立策略',
    'acRate': '接通电源目标刷新率',
    'batteryRate': '电池目标刷新率',
    'enableAnimations': '启用动画控制',
    'acPolicy': '接通电源策略',
    'batteryPolicy': '电池策略',
    'currentDisplay': '当前显示模式',
    'unavailable': '刷新率不可用',
    'noRates': '未报告可用刷新率',
    'selectRate': '选择刷新率',
    'connected': '原生服务已连接',
    'connecting': '正在连接原生服务...',
    'applyingStatus': '正在通过原生服务应用更改...',
    'applied': '更改已应用',
    'failed': '未应用更改',
    'nativeUnavailable': '原生服务不可用',
    'retry': '重试连接',
    'window': '最小化/最大化窗口动画',
    'taskbar': '任务栏和缩略图预览动画',
    'menu': '菜单动画',
    'combo': '组合框动画',
    'list': '列表框平滑滚动',
    'selection': '选择淡化',
    'tooltip': '工具提示动画',
    'tooltipFade': '工具提示淡化',
    'cursor': '光标阴影',
    'ui': 'UI 效果',
    'client': '客户端区域动画',
    'overlap': '禁用重叠内容',
  };
  String operator [](String key) =>
      (lang == AppLanguage.zh ? _zh : _en)[key] ?? key;
}

class PowerProfileApp extends StatelessWidget {
  const PowerProfileApp({required this.service, super.key});
  final PowerProfileService service;
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'PowerProfile',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      brightness: Brightness.dark,
      scaffoldBackgroundColor: const Color(0xff0b111c),
      colorScheme: ColorScheme.fromSeed(
        seedColor: const Color(0xff35a9ff),
        brightness: Brightness.dark,
      ),
      fontFamily: 'Segoe UI',
      useMaterial3: true,
    ),
    home: ProfilePage(service: service),
  );
}

class ProfilePage extends StatefulWidget {
  const ProfilePage({required this.service, super.key});
  final PowerProfileService service;
  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  ProfileState _state = const ProfileState();
  bool _loading = true, _applying = false;
  String? _error;
  String _status = '';
  PageSection _section = PageSection.overview;
  AppLanguage _language = AppLanguage.en;
  Texts get t => Texts(_language);
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
      _status = t['connecting'];
    });
    try {
      final s = await widget.service.load();
      if (mounted)
        setState(() {
          _state = s;
          _loading = false;
          _status = '${t['connected']} · ${_source(s.powerSource)}';
        });
    } catch (e) {
      if (mounted)
        setState(() {
          _loading = false;
          _error = e.toString();
          _status = t['nativeUnavailable'];
        });
    }
  }

  Future<void> _apply() async {
    if (_applying) return;
    setState(() {
      _applying = true;
      _error = null;
      _status = t['applyingStatus'];
    });
    try {
      final s = await widget.service.apply(_state);
      if (mounted)
        setState(() {
          _state = s;
          _status = '${t['applied']} · ${_source(s.powerSource)}';
        });
    } catch (e) {
      if (mounted)
        setState(() {
          _error = e.toString();
          _status = t['failed'];
        });
    } finally {
      if (mounted) setState(() => _applying = false);
    }
  }

  String _source(String source) => source == 'AC'
      ? 'AC'
      : source == 'Battery'
      ? '${_language == AppLanguage.zh ? '电池' : 'Battery'}'
      : _language == AppLanguage.zh
      ? '未知'
      : 'Unknown';
  void _update(ProfileState s) {
    if (!_applying) setState(() => _state = s);
  }

  @override
  Widget build(BuildContext context) {
    final wide = MediaQuery.sizeOf(context).width >= 820;
    return Scaffold(
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : SafeArea(
              child: Row(
                children: [
                  if (wide) _sidebar(),
                  Expanded(
                    child: CustomScrollView(
                      slivers: [
                        SliverToBoxAdapter(child: _header()),
                        SliverPadding(
                          padding: const EdgeInsets.fromLTRB(28, 8, 28, 28),
                          sliver: SliverList(
                            delegate: SliverChildListDelegate([
                              if (_error != null) _errorBanner(),
                              if (_error != null) const SizedBox(height: 18),
                              ..._content(wide),
                              const SizedBox(height: 18),
                              _footer(),
                            ]),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  List<Widget> _content(bool wide) {
    final cards = <Widget>[];
    if (_section == PageSection.overview) {
      cards.add(_overview());
      cards.add(const SizedBox(height: 18));
      cards.add(
        wide
            ? Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: _refreshCard()),
                  const SizedBox(width: 18),
                  Expanded(child: _animationCard()),
                ],
              )
            : Column(
                children: [
                  _refreshCard(),
                  const SizedBox(height: 18),
                  _animationCard(),
                ],
              ),
      );
    } else if (_section == PageSection.refresh) {
      cards.add(_refreshCard());
    } else {
      cards.add(_animationCard());
    }
    return cards;
  }

  Widget _sidebar() => Container(
    width: 230,
    padding: const EdgeInsets.fromLTRB(22, 28, 14, 22),
    decoration: const BoxDecoration(
      color: Color(0xff101a29),
      border: Border(right: BorderSide(color: Color(0xff1c2a3d))),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Image.asset('assets/PowerProfile-icon.png', width: 36, height: 36),
            const SizedBox(width: 10),
            const Text(
              'PowerProfile',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
            ),
          ],
        ),
        const SizedBox(height: 30),
        _nav(PageSection.overview, Icons.dashboard_rounded, t['overview']),
        _nav(PageSection.refresh, Icons.speed_rounded, t['refresh']),
        _nav(
          PageSection.animations,
          Icons.auto_awesome_rounded,
          t['animations'],
        ),
        const Spacer(),
        Text(
          t['language'],
          style: const TextStyle(
            color: Color(0xff65809c),
            fontSize: 10,
            letterSpacing: 1.2,
          ),
        ),
        DropdownButton<AppLanguage>(
          value: _language,
          isExpanded: true,
          onChanged: (v) {
            if (v != null) setState(() => _language = v);
          },
          items: [
            DropdownMenuItem(value: AppLanguage.zh, child: Text(t['chinese'])),
            DropdownMenuItem(value: AppLanguage.en, child: Text(t['english'])),
          ],
        ),
        Text(
          _status,
          style: const TextStyle(
            color: Color(0xff8ca1b8),
            height: 1.5,
            fontSize: 11,
          ),
        ),
      ],
    ),
  );
  Widget _nav(PageSection s, IconData icon, String label) {
    final selected = s == _section;
    return Semantics(
      selected: selected,
      button: true,
      label: label,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        decoration: BoxDecoration(
          color: selected ? const Color(0xff173b5b) : Colors.transparent,
          borderRadius: BorderRadius.circular(10),
        ),
        child: ListTile(
          leading: Icon(
            icon,
            size: 19,
            color: selected ? const Color(0xff50c4ff) : const Color(0xff7890a9),
          ),
          title: Text(label),
          onTap: () => setState(() => _section = s),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
    );
  }

  Widget _header() => Padding(
    padding: const EdgeInsets.fromLTRB(28, 28, 28, 14),
    child: Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'PowerProfile',
                style: TextStyle(color: Color(0xff8398b0), fontSize: 13),
              ),
              const SizedBox(height: 4),
              Text(
                t['dashboard'],
                style: const TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                t['subtitle'],
                style: const TextStyle(color: Color(0xff8398b0), fontSize: 12),
              ),
            ],
          ),
        ),
        _chip(
          Icons.bolt_rounded,
          _source(_state.powerSource).toUpperCase(),
          const Color(0xff3dd6a3),
        ),
      ],
    ),
  );
  Widget _chip(IconData i, String text, Color c) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 8),
    decoration: BoxDecoration(
      color: c.withAlpha(25),
      borderRadius: BorderRadius.circular(20),
      border: Border.all(color: c.withAlpha(85)),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(i, size: 14, color: c),
        const SizedBox(width: 6),
        Text(
          text,
          style: TextStyle(color: c, fontSize: 10, fontWeight: FontWeight.w700),
        ),
      ],
    ),
  );
  Widget _overview() => Container(
    padding: const EdgeInsets.all(24),
    decoration: BoxDecoration(
      gradient: const LinearGradient(
        colors: [Color(0xff12365c), Color(0xff142238)],
      ),
      borderRadius: BorderRadius.circular(18),
    ),
    child: Row(
      children: [
        const Icon(Icons.monitor_rounded, size: 42, color: Color(0xff55c8ff)),
        const SizedBox(width: 18),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                t['currentDisplay'],
                style: const TextStyle(
                  color: Color(0xff75b6dc),
                  fontSize: 10,
                  letterSpacing: 1.2,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 7),
              Text(
                _state.currentRefreshRate > 0
                    ? '${_state.currentRefreshRate} Hz'
                    : t['unavailable'],
                style: const TextStyle(
                  fontSize: 21,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 5),
              Text(
                _status,
                style: const TextStyle(color: Color(0xffa2bad0), fontSize: 12),
              ),
            ],
          ),
        ),
      ],
    ),
  );
  Widget _card(Widget child) => Material(
    color: const Color(0xff121d2c),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(16),
      side: const BorderSide(color: Color(0xff1f3045)),
    ),
    child: Padding(padding: const EdgeInsets.all(20), child: child),
  );
  Widget _refreshCard() => _card(
    Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _title(Icons.speed_rounded, t['refresh']),
        _toggle(
          t['enableRefresh'],
          _state.refreshRateEnabled,
          (v) => _update(_state.copyWith(refreshRateEnabled: v)),
        ),
        _toggle(
          t['powerPolicy'],
          _state.refreshRateAutoSwitch,
          _state.refreshRateEnabled
              ? (v) => _update(_state.copyWith(refreshRateAutoSwitch: v))
              : null,
        ),
        _rate(
          t['acRate'],
          _state.refreshRateAc,
          (v) => _update(_state.copyWith(refreshRateAc: v)),
        ),
        if (_state.refreshRateAutoSwitch)
          _rate(
            t['batteryRate'],
            _state.refreshRateBattery,
            (v) => _update(_state.copyWith(refreshRateBattery: v)),
          ),
      ],
    ),
  );
  Widget _rate(String label, int current, ValueChanged<int> changed) => Padding(
    padding: const EdgeInsets.only(top: 12),
    child: DropdownButtonFormField<int>(
      key: Key(label),
      value: _state.availableRefreshRates.contains(current) ? current : null,
      decoration: _input(label),
      hint: Text(
        _state.availableRefreshRates.isEmpty ? t['noRates'] : t['selectRate'],
      ),
      items: _state.availableRefreshRates
          .map((r) => DropdownMenuItem(value: r, child: Text('$r Hz')))
          .toList(),
      onChanged: _state.refreshRateEnabled && !_applying
          ? (v) {
              if (v != null) changed(v);
            }
          : null,
    ),
  );
  Widget _animationCard() {
    final battery = _state.animationAutoSwitch;
    return _card(
      Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _title(Icons.auto_awesome_rounded, t['animations']),
          _toggle(
            t['enableAnimations'],
            _state.animationEnabled,
            (v) => _update(_state.copyWith(animationEnabled: v)),
          ),
          _toggle(
            t['powerPolicy'],
            _state.animationAutoSwitch,
            _state.animationEnabled
                ? (v) => _update(_state.copyWith(animationAutoSwitch: v))
                : null,
          ),
          const SizedBox(height: 8),
          _policy(t['acPolicy'], _state.animationPolicyAc, false),
          if (battery)
            _policy(t['batteryPolicy'], _state.animationPolicyBattery, true),
        ],
      ),
    );
  }

  Widget _policy(String title, AnimationPolicy p, bool battery) =>
      ExpansionTile(
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
        children: _effects
            .map(
              (e) => _toggle(
                t[e.$1],
                _effect(p, e.$2),
                _state.animationEnabled && !_applying
                    ? (v) => _setEffect(battery, e.$2, v)
                    : null,
              ),
            )
            .toList(),
      );
  static const _effects = [
    ('window', 'windowAnimation'),
    ('taskbar', 'taskbarAnimations'),
    ('menu', 'menuAnimation'),
    ('combo', 'comboBoxAnimation'),
    ('list', 'listBoxSmoothScrolling'),
    ('selection', 'selectionFade'),
    ('tooltip', 'tooltipAnimation'),
    ('tooltipFade', 'tooltipFade'),
    ('cursor', 'cursorShadow'),
    ('ui', 'uiEffects'),
    ('client', 'clientAreaAnimation'),
    ('overlap', 'disableOverlappedContent'),
  ];
  bool _effect(AnimationPolicy p, String f) => p.toJson()[f] as bool;
  void _setEffect(bool battery, String field, bool value) {
    final policy =
        (battery ? _state.animationPolicyBattery : _state.animationPolicyAc)
            .copyWith(fieldName: field, value: value);
    _update(
      battery
          ? _state.copyWith(animationPolicyBattery: policy)
          : _state.copyWith(animationPolicyAc: policy),
    );
  }

  Widget _title(IconData i, String title) => Row(
    children: [
      Icon(i, color: const Color(0xff53c5ff)),
      const SizedBox(width: 10),
      Text(
        title,
        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
      ),
    ],
  );
  Widget _toggle(String label, bool value, ValueChanged<bool>? change) =>
      SwitchListTile.adaptive(
        contentPadding: EdgeInsets.zero,
        dense: true,
        title: Text(label, style: const TextStyle(fontSize: 13)),
        value: value,
        onChanged: change,
        activeThumbColor: const Color(0xff43b9ff),
      );
  InputDecoration _input(String label) => InputDecoration(
    labelText: label,
    filled: true,
    fillColor: const Color(0xff0c1623),
    border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
  );
  Widget _errorBanner() => Container(
    padding: const EdgeInsets.all(14),
    color: const Color(0xff4b2a22),
    child: Row(
      children: [
        const Icon(Icons.error_outline_rounded),
        const SizedBox(width: 10),
        Expanded(child: Text(_error!)),
        TextButton(onPressed: _load, child: Text(t['retry'])),
      ],
    ),
  );
  Widget _footer() => Row(
    children: [
      Expanded(
        child: Text(
          _status,
          style: const TextStyle(color: Color(0xff70869d), fontSize: 11),
        ),
      ),
      FilledButton.icon(
        onPressed: _applying ? null : _apply,
        icon: const Icon(Icons.check_rounded),
        label: Text(_applying ? t['applying'] : t['apply']),
      ),
    ],
  );
}
