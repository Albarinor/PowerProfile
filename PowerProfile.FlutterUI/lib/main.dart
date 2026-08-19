import 'dart:io';

import 'package:flutter/material.dart';

import 'power_profile_service.dart';

void main() => runApp(PowerProfileApp(
      service: Platform.isWindows ? WindowsPowerProfileService() : MockPowerProfileService(),
    ));

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
          colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff35a9ff), brightness: Brightness.dark),
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
  bool _loading = true;
  bool _applying = false;
  String _status = 'Connecting to native service...';
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
      _status = 'Connecting to native service...';
    });
    try {
      final value = await widget.service.load();
      if (!mounted) return;
      setState(() {
        _state = value;
        _loading = false;
        _status = _readyStatus(value);
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = error.toString();
        _status = 'Native service unavailable';
      });
    }
  }

  String _readyStatus(ProfileState state) =>
      'Native service connected · ${state.powerSource} power';

  void _update(ProfileState value) {
    if (!_applying) {
      setState(() => _state = value);
    }
  }

  Future<void> _apply() async {
    if (_applying) return;
    final requestedState = _state;
    setState(() {
      _applying = true;
      _error = null;
      _status = 'Applying changes through the native service...';
    });
    try {
      final value = await widget.service.apply(requestedState);
      if (!mounted) return;
      setState(() {
        _state = value;
        _status = 'Changes applied · ${value.powerSource} power';
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = error.toString();
        _status = 'Changes were not applied';
      });
    } finally {
      if (mounted) setState(() => _applying = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final wide = MediaQuery.sizeOf(context).width >= 820;
    return Scaffold(
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && _state.availableRefreshRates.isEmpty
              ? _connectionError()
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
                                  _overviewCard(),
                                  const SizedBox(height: 18),
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
                                  const SizedBox(height: 18),
                                  _footerBar(),
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

  Widget _connectionError() => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            const Icon(Icons.link_off_rounded, size: 48, color: Color(0xffffa15c)),
            const SizedBox(height: 16),
            const Text('Native service unavailable', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            Text(_error ?? 'Start the PowerProfile host and try again.', textAlign: TextAlign.center, style: const TextStyle(color: Color(0xffa2bad0))),
            const SizedBox(height: 20),
            FilledButton.icon(onPressed: _load, icon: const Icon(Icons.refresh_rounded), label: const Text('Retry connection')),
          ]),
        ),
      );

  Widget _errorBanner() => Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(color: const Color(0xff4b2a22), borderRadius: BorderRadius.circular(12), border: Border.all(color: const Color(0xffa9583b))),
        child: Row(children: [
          const Icon(Icons.error_outline_rounded, color: Color(0xffffae76)),
          const SizedBox(width: 10),
          Expanded(child: Text(_error!, style: const TextStyle(color: Color(0xffffd6bf), fontSize: 12))),
          TextButton(onPressed: _applying ? null : _load, child: const Text('Retry')),
        ]),
      );

  Widget _sidebar() => Container(
        width: 220,
        padding: const EdgeInsets.fromLTRB(22, 28, 14, 22),
        decoration: const BoxDecoration(color: Color(0xff101a29), border: Border(right: BorderSide(color: Color(0xff1c2a3d)))),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Image.asset('assets/PowerProfile-icon.png', width: 36, height: 36),
            const SizedBox(width: 10),
            const Text('PowerProfile', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
          ]),
          const SizedBox(height: 38),
          _navItem(Icons.dashboard_rounded, 'Overview', true),
          _navItem(Icons.speed_rounded, 'Refresh rate', false),
          _navItem(Icons.auto_awesome_rounded, 'Animations', false),
          const Spacer(),
          const Text('NATIVE SERVICE', style: TextStyle(color: Color(0xff65809c), fontSize: 10, letterSpacing: 1.2)),
          const SizedBox(height: 8),
          Text(_status, style: const TextStyle(color: Color(0xff8ca1b8), height: 1.5, fontSize: 11)),
        ]),
      );

  Widget _navItem(IconData icon, String label, bool selected) => Container(
        margin: const EdgeInsets.only(bottom: 8),
        decoration: BoxDecoration(color: selected ? const Color(0xff173b5b) : Colors.transparent, borderRadius: BorderRadius.circular(10)),
        child: ListTile(
          dense: true,
          leading: Icon(icon, size: 19, color: selected ? const Color(0xff50c4ff) : const Color(0xff7890a9)),
          title: Text(label, style: TextStyle(color: selected ? Colors.white : const Color(0xff9caec2), fontSize: 13)),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      );

  Widget _header() => Padding(
        padding: const EdgeInsets.fromLTRB(28, 28, 28, 14),
        child: Row(children: [
          const Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('PowerProfile', style: TextStyle(color: Color(0xff8398b0), fontSize: 13)),
            SizedBox(height: 4),
            Text('Power dashboard', style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700)),
            SizedBox(height: 4),
            Text('Display and Windows animation controls', style: TextStyle(color: Color(0xff8398b0), fontSize: 12)),
          ])),
          _statusChip(Icons.bolt_rounded, _state.powerSource.toUpperCase(), const Color(0xff3dd6a3)),
        ]),
      );

  Widget _statusChip(IconData icon, String text, Color color) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 8),
        decoration: BoxDecoration(color: color.withAlpha(25), borderRadius: BorderRadius.circular(20), border: Border.all(color: color.withAlpha(85))),
        child: Row(mainAxisSize: MainAxisSize.min, children: [Icon(icon, size: 14, color: color), const SizedBox(width: 6), Text(text, style: TextStyle(color: color, fontSize: 10, fontWeight: FontWeight.w700, letterSpacing: .7))]),
      );

  Widget _overviewCard() => Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(gradient: const LinearGradient(colors: [Color(0xff12365c), Color(0xff142238)], begin: Alignment.topLeft, end: Alignment.bottomRight), borderRadius: BorderRadius.circular(18), border: Border.all(color: const Color(0xff23577f))),
        child: Row(children: [
          Container(width: 62, height: 62, decoration: BoxDecoration(color: const Color(0xff36b7ff).withAlpha(35), shape: BoxShape.circle), child: const Icon(Icons.battery_charging_full_rounded, size: 32, color: Color(0xff55c8ff))),
          const SizedBox(width: 18),
          Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('CURRENT DISPLAY', style: TextStyle(color: Color(0xff75b6dc), fontSize: 10, letterSpacing: 1.2, fontWeight: FontWeight.w700)),
            const SizedBox(height: 7),
            Text(_state.currentRefreshRate > 0 ? '${_state.currentRefreshRate} Hz' : 'Refresh rate unavailable', style: const TextStyle(fontSize: 21, fontWeight: FontWeight.w700)),
            const SizedBox(height: 5),
            Text(_status, style: const TextStyle(color: Color(0xffa2bad0), fontSize: 12)),
          ])),
        ]),
      );

  Widget _card({required Widget child}) => Material(
        color: const Color(0xff121d2c),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16), side: const BorderSide(color: Color(0xff1f3045))),
        child: Padding(padding: const EdgeInsets.all(20), child: child),
      );

  Widget _refreshCard() => _card(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        _cardTitle(Icons.speed_rounded, 'Refresh rate', 'Use display modes reported by the native service.'),
        const SizedBox(height: 17),
        _toggleRow('Enable refresh-rate control', _state.refreshRateEnabled, _applying ? null : (v) => _update(_state.copyWith(refreshRateEnabled: v))),
        _toggleRow('Auto-switch by power source', _state.refreshRateAutoSwitch, _state.refreshRateEnabled && !_applying ? (v) => _update(_state.copyWith(refreshRateAutoSwitch: v)) : null),
        const SizedBox(height: 12),
        _refreshRateDropdown('AC target rate', _state.refreshRateAc, (value) => _update(_state.copyWith(refreshRateAc: value))),
        const SizedBox(height: 12),
        _refreshRateDropdown('Battery target rate', _state.refreshRateBattery, (value) => _update(_state.copyWith(refreshRateBattery: value))),
      ]));

  Widget _refreshRateDropdown(String label, int configuredValue, ValueChanged<int> onChanged) {
    final rates = _state.availableRefreshRates;
    final selected = rates.contains(configuredValue) ? configuredValue : null;
    final enabled = _state.refreshRateEnabled && !_applying && rates.isNotEmpty;
    return DropdownButtonFormField<int>(
      key: Key(label),
      decoration: _input(rates.isEmpty ? '$label (unavailable)' : label),
      initialValue: selected,
      hint: Text(rates.isEmpty ? 'No refresh rates reported' : 'Select a rate'),
      items: rates.map((rate) => DropdownMenuItem(value: rate, child: Text('$rate Hz'))).toList(),
      onChanged: enabled ? (value) { if (value != null) onChanged(value); } : null,
    );
  }

  Widget _animationCard() => _card(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        _cardTitle(Icons.auto_awesome_rounded, 'Animations', 'Apply Windows visual effects for each power source.'),
        const SizedBox(height: 17),
        _toggleRow('Enable animation control', _state.animationEnabled, _applying ? null : (v) => _update(_state.copyWith(animationEnabled: v))),
        _toggleRow('Auto-switch by power source', _state.animationAutoSwitch, _state.animationEnabled && !_applying ? (v) => _update(_state.copyWith(animationAutoSwitch: v)) : null),
        const SizedBox(height: 8),
        _toggleRow('Animations on AC', _state.animationsOnAc, _state.animationEnabled && !_applying ? (v) => _update(_state.copyWith(animationsOnAc: v)) : null),
        _toggleRow('Animations on battery', _state.animationsOnBattery, _state.animationEnabled && !_applying ? (v) => _update(_state.copyWith(animationsOnBattery: v)) : null),
      ]));

  Widget _cardTitle(IconData icon, String title, String subtitle) => Row(crossAxisAlignment: CrossAxisAlignment.start, children: [Icon(icon, color: const Color(0xff53c5ff), size: 22), const SizedBox(width: 10), Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)), const SizedBox(height: 4), Text(subtitle, style: const TextStyle(color: Color(0xff8299b0), fontSize: 11))]))]);

  Widget _toggleRow(String label, bool value, ValueChanged<bool>? onChanged) => SwitchListTile.adaptive(contentPadding: EdgeInsets.zero, dense: true, title: Text(label, style: const TextStyle(fontSize: 13)), value: value, onChanged: onChanged, activeThumbColor: const Color(0xff43b9ff));

  InputDecoration _input(String label) => InputDecoration(labelText: label, filled: true, fillColor: const Color(0xff0c1623), border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Color(0xff293d53))), enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Color(0xff293d53))));

  Widget _footerBar() => Row(children: [
        Expanded(child: Text('Native Windows service · ${_state.currentRefreshRate > 0 ? '${_state.currentRefreshRate} Hz active' : 'display mode unavailable'}', style: const TextStyle(color: Color(0xff70869d), fontSize: 11))),
        FilledButton.icon(onPressed: _applying ? null : _apply, icon: _applying ? const SizedBox(width: 17, height: 17, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check_rounded, size: 17), label: Text(_applying ? 'Applying...' : 'Apply changes')),
      ]);
}
