import 'dart:convert';
import 'dart:io';

import 'package:flutter/services.dart';

class AnimationPolicy {
  const AnimationPolicy({
    this.windowAnimation = true,
    this.taskbarAnimations = true,
    this.menuAnimation = true,
    this.comboBoxAnimation = true,
    this.listBoxSmoothScrolling = true,
    this.selectionFade = true,
    this.tooltipAnimation = true,
    this.tooltipFade = true,
    this.cursorShadow = true,
    this.uiEffects = true,
    this.clientAreaAnimation = true,
    this.disableOverlappedContent = false,
  });

  final bool windowAnimation,
      taskbarAnimations,
      menuAnimation,
      comboBoxAnimation,
      listBoxSmoothScrolling;
  final bool selectionFade,
      tooltipAnimation,
      tooltipFade,
      cursorShadow,
      uiEffects,
      clientAreaAnimation,
      disableOverlappedContent;

  factory AnimationPolicy.fromJson(
    Object? value, {
    required bool legacyEnabled,
  }) {
    if (value is! Map) return AnimationPolicy.fromLegacy(legacyEnabled);
    final json = Map<String, dynamic>.from(value);
    bool field(String name, bool fallback) =>
        json[name] is bool ? json[name] as bool : fallback;
    return AnimationPolicy(
      windowAnimation: field('windowAnimation', legacyEnabled),
      taskbarAnimations: field('taskbarAnimations', legacyEnabled),
      menuAnimation: field('menuAnimation', legacyEnabled),
      comboBoxAnimation: field('comboBoxAnimation', legacyEnabled),
      listBoxSmoothScrolling: field('listBoxSmoothScrolling', legacyEnabled),
      selectionFade: field('selectionFade', legacyEnabled),
      tooltipAnimation: field('tooltipAnimation', legacyEnabled),
      tooltipFade: field('tooltipFade', legacyEnabled),
      cursorShadow: field('cursorShadow', legacyEnabled),
      uiEffects: field('uiEffects', legacyEnabled),
      clientAreaAnimation: field('clientAreaAnimation', legacyEnabled),
      disableOverlappedContent: field(
        'disableOverlappedContent',
        !legacyEnabled,
      ),
    );
  }

  factory AnimationPolicy.fromLegacy(bool enabled) => AnimationPolicy(
    windowAnimation: enabled,
    taskbarAnimations: enabled,
    menuAnimation: enabled,
    comboBoxAnimation: enabled,
    listBoxSmoothScrolling: enabled,
    selectionFade: enabled,
    tooltipAnimation: enabled,
    tooltipFade: enabled,
    cursorShadow: enabled,
    uiEffects: enabled,
    clientAreaAnimation: enabled,
    disableOverlappedContent: !enabled,
  );

  AnimationPolicy copyWith({String? fieldName, bool? value}) {
    if (fieldName == null || value == null) return this;
    final json = toJson()..[fieldName] = value;
    return AnimationPolicy.fromJson(json, legacyEnabled: true);
  }

  Map<String, dynamic> toJson() => {
    'windowAnimation': windowAnimation,
    'taskbarAnimations': taskbarAnimations,
    'menuAnimation': menuAnimation,
    'comboBoxAnimation': comboBoxAnimation,
    'listBoxSmoothScrolling': listBoxSmoothScrolling,
    'selectionFade': selectionFade,
    'tooltipAnimation': tooltipAnimation,
    'tooltipFade': tooltipFade,
    'cursorShadow': cursorShadow,
    'uiEffects': uiEffects,
    'clientAreaAnimation': clientAreaAnimation,
    'disableOverlappedContent': disableOverlappedContent,
  };
}

class ProfileState {
  const ProfileState({
    this.powerSource = 'Unknown',
    this.currentRefreshRate = 0,
    this.availableRefreshRates = const [],
    this.refreshRateEnabled = true,
    this.refreshRateAutoSwitch = true,
    this.refreshRateAc = 0,
    this.refreshRateBattery = 0,
    this.animationEnabled = true,
    this.animationAutoSwitch = true,
    this.animationsOnAc = true,
    this.animationsOnBattery = false,
    this.animationPolicyAc = const AnimationPolicy(),
    this.animationPolicyBattery = const AnimationPolicy(
      windowAnimation: false,
      taskbarAnimations: false,
      menuAnimation: false,
      comboBoxAnimation: false,
      listBoxSmoothScrolling: false,
      selectionFade: false,
      tooltipAnimation: false,
      tooltipFade: false,
      cursorShadow: false,
      uiEffects: false,
      clientAreaAnimation: false,
      disableOverlappedContent: true,
    ),
    this.uiLanguage = 'en',
  });
  final String powerSource;
  final int currentRefreshRate;
  final List<int> availableRefreshRates;
  final bool refreshRateEnabled,
      refreshRateAutoSwitch,
      animationEnabled,
      animationAutoSwitch,
      animationsOnAc,
      animationsOnBattery;
  final int refreshRateAc, refreshRateBattery;
  final AnimationPolicy animationPolicyAc, animationPolicyBattery;
  final String uiLanguage;

  factory ProfileState.fromJson(Map<String, dynamic> json) {
    final settings = _map(json['settings'], 'settings');
    final ratesValue = json['availableRefreshRates'];
    if (ratesValue is! List)
      throw const FormatException(
        'The backend response has no refresh-rate list.',
      );
    final rates =
        ratesValue
            .map((value) => _integer(value, 'availableRefreshRates'))
            .toSet()
            .toList()
          ..sort();
    final onAc = _boolean(
      settings['animationsOnAC'],
      'settings.animationsOnAC',
    );
    final onBattery = _boolean(
      settings['animationsOnBattery'],
      'settings.animationsOnBattery',
    );
    return ProfileState(
      powerSource: _string(json['powerSource'], 'powerSource'),
      currentRefreshRate: _integer(
        json['currentRefreshRate'],
        'currentRefreshRate',
      ),
      availableRefreshRates: List.unmodifiable(rates),
      refreshRateEnabled: _boolean(
        settings['refreshRateEnabled'],
        'settings.refreshRateEnabled',
      ),
      refreshRateAutoSwitch: _boolean(
        settings['refreshRateAutoSwitch'],
        'settings.refreshRateAutoSwitch',
      ),
      refreshRateAc: _integer(
        settings['refreshRateAC'],
        'settings.refreshRateAC',
      ),
      refreshRateBattery: _integer(
        settings['refreshRateBattery'],
        'settings.refreshRateBattery',
      ),
      animationEnabled: _boolean(
        settings['animationsEnabled'],
        'settings.animationsEnabled',
      ),
      animationAutoSwitch: _boolean(
        settings['animationsAutoSwitch'],
        'settings.animationsAutoSwitch',
      ),
      animationsOnAc: onAc,
      animationsOnBattery: onBattery,
      animationPolicyAc: AnimationPolicy.fromJson(
        settings['animationPolicyAC'],
        legacyEnabled: onAc,
      ),
      animationPolicyBattery: AnimationPolicy.fromJson(
        settings['animationPolicyBattery'],
        legacyEnabled: onBattery,
      ),
      uiLanguage: settings['uiLanguage'] is String
          ? settings['uiLanguage'] as String
          : 'en',
    );
  }
  static Map<String, dynamic> _map(Object? value, String name) {
    if (value is Map<String, dynamic>) return value;
    if (value is Map) return Map<String, dynamic>.from(value);
    throw FormatException('The backend response has an invalid $name object.');
  }

  static String _string(Object? value, String name) {
    if (value is String && value.isNotEmpty) return value;
    throw FormatException('The backend response has an invalid $name value.');
  }

  static int _integer(Object? value, String name) {
    if (value is int && value >= 0) return value;
    if (value is num && value >= 0 && value == value.roundToDouble())
      return value.toInt();
    throw FormatException('The backend response has an invalid $name value.');
  }

  static bool _boolean(Object? value, String name) {
    if (value is bool) return value;
    throw FormatException('The backend response has an invalid $name value.');
  }

  ProfileState copyWith({
    bool? refreshRateEnabled,
    bool? refreshRateAutoSwitch,
    int? refreshRateAc,
    int? refreshRateBattery,
    bool? animationEnabled,
    bool? animationAutoSwitch,
    bool? animationsOnAc,
    bool? animationsOnBattery,
    AnimationPolicy? animationPolicyAc,
    AnimationPolicy? animationPolicyBattery,
    String? uiLanguage,
  }) => ProfileState(
    powerSource: powerSource,
    currentRefreshRate: currentRefreshRate,
    availableRefreshRates: availableRefreshRates,
    refreshRateEnabled: refreshRateEnabled ?? this.refreshRateEnabled,
    refreshRateAutoSwitch: refreshRateAutoSwitch ?? this.refreshRateAutoSwitch,
    refreshRateAc: refreshRateAc ?? this.refreshRateAc,
    refreshRateBattery: refreshRateBattery ?? this.refreshRateBattery,
    animationEnabled: animationEnabled ?? this.animationEnabled,
    animationAutoSwitch: animationAutoSwitch ?? this.animationAutoSwitch,
    animationsOnAc: animationsOnAc ?? this.animationsOnAc,
    animationsOnBattery: animationsOnBattery ?? this.animationsOnBattery,
    animationPolicyAc: animationPolicyAc ?? this.animationPolicyAc,
    animationPolicyBattery:
        animationPolicyBattery ?? this.animationPolicyBattery,
    uiLanguage: uiLanguage ?? this.uiLanguage,
  );
  Map<String, dynamic> toApplyJson() => {
    'refreshRateEnabled': refreshRateEnabled,
    'refreshRateAutoSwitch': refreshRateAutoSwitch,
    'refreshRateAC': refreshRateAc,
    'refreshRateBattery': refreshRateBattery,
    'animationsEnabled': animationEnabled,
    'animationsAutoSwitch': animationAutoSwitch,
    'animationsOnAC': animationsOnAc,
    'animationsOnBattery': animationsOnBattery,
    'animationPolicyAC': animationPolicyAc.toJson(),
    'animationPolicyBattery': animationPolicyBattery.toJson(),
    'uiLanguage': uiLanguage,
  };
}

abstract interface class PowerProfileService {
  Future<ProfileState> load();
  Future<ProfileState> apply(ProfileState state);
  Future<ProfileState> setLanguage(String language);
}

class PowerProfileConnectionException implements Exception {
  PowerProfileConnectionException(this.message);
  final String message;
  @override
  String toString() => message;
}

class PowerProfileCommandException implements Exception {
  PowerProfileCommandException(this.code, this.message);
  final String code;
  final String message;
  @override
  String toString() => message;
}

class WindowsPowerProfileService implements PowerProfileService {
  static const _channel = MethodChannel('powerprofile/local_pipe');
  @override
  Future<ProfileState> load() =>
      _request('getState').then(ProfileState.fromJson);
  @override
  Future<ProfileState> apply(ProfileState state) =>
      _request('apply', state.toApplyJson()).then(ProfileState.fromJson);
  @override
  Future<ProfileState> setLanguage(String language) => _request('setLanguage', {
    'uiLanguage': language,
  }).then(ProfileState.fromJson);
  Future<Map<String, dynamic>> _request(
    String command, [
    Map<String, dynamic>? payload,
  ]) async {
    if (!Platform.isWindows)
      throw PowerProfileConnectionException(
        'The Windows backend is unavailable on this platform.',
      );
    final requestPayload = <String, dynamic>{
      'id': DateTime.now().microsecondsSinceEpoch.toString(),
      'version': 1,
      'command': command,
    };
    if (payload != null) requestPayload['payload'] = payload;
    try {
      final raw = await _channel.invokeMethod<String>(
        'request',
        jsonEncode(requestPayload),
      );
      if (raw == null)
        throw PowerProfileConnectionException(
          'The native pipe returned no response.',
        );
      final decoded = jsonDecode(raw);
      if (decoded is! Map)
        throw const FormatException(
          'The native pipe returned an invalid response.',
        );
      final response = Map<String, dynamic>.from(decoded);
      if (response['ok'] != true) {
        final error = response['error'];
        final message = error is Map ? error['message'] : null;
        final code = error is Map ? error['code'] : null;
        throw PowerProfileCommandException(
          code is String ? code : 'backend_rejected',
          message is String ? message : 'The backend rejected the request.',
        );
      }
      final data = response['data'];
      if (data is! Map)
        throw const FormatException(
          'The native pipe response has no state data.',
        );
      return Map<String, dynamic>.from(data);
    } on PlatformException catch (error) {
      throw PowerProfileConnectionException(
        error.message ?? 'Unable to connect to the PowerProfile host.',
      );
    } on FormatException catch (error) {
      throw PowerProfileConnectionException(error.message);
    }
  }
}

class MockPowerProfileService implements PowerProfileService {
  ProfileState _state = const ProfileState(
    powerSource: 'AC',
    currentRefreshRate: 120,
    availableRefreshRates: [60, 90, 120, 144],
    refreshRateAc: 120,
    refreshRateBattery: 60,
  );
  @override
  Future<ProfileState> load() async => _state;
  @override
  Future<ProfileState> apply(ProfileState state) async => _state = state;
  @override
  Future<ProfileState> setLanguage(String language) async =>
      _state = _state.copyWith(uiLanguage: language);
}
