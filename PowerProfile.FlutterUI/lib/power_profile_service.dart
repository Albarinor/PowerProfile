import 'dart:convert';
import 'dart:io';

import 'package:flutter/services.dart';

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
  });

  final String powerSource;
  final int currentRefreshRate;
  final List<int> availableRefreshRates;
  final bool refreshRateEnabled;
  final bool refreshRateAutoSwitch;
  final int refreshRateAc;
  final int refreshRateBattery;
  final bool animationEnabled;
  final bool animationAutoSwitch;
  final bool animationsOnAc;
  final bool animationsOnBattery;

  factory ProfileState.fromJson(Map<String, dynamic> json) {
    final settings = _map(json['settings'], 'settings');
    final ratesValue = json['availableRefreshRates'];
    if (ratesValue is! List) {
      throw const FormatException('The backend response has no refresh-rate list.');
    }

    final rates = ratesValue.map((value) => _integer(value, 'availableRefreshRates')).toSet().toList()..sort();
    return ProfileState(
      powerSource: _string(json['powerSource'], 'powerSource'),
      currentRefreshRate: _integer(json['currentRefreshRate'], 'currentRefreshRate'),
      availableRefreshRates: List.unmodifiable(rates),
      refreshRateEnabled: _boolean(settings['refreshRateEnabled'], 'settings.refreshRateEnabled'),
      refreshRateAutoSwitch: _boolean(settings['refreshRateAutoSwitch'], 'settings.refreshRateAutoSwitch'),
      refreshRateAc: _integer(settings['refreshRateAC'], 'settings.refreshRateAC'),
      refreshRateBattery: _integer(settings['refreshRateBattery'], 'settings.refreshRateBattery'),
      animationEnabled: _boolean(settings['animationsEnabled'], 'settings.animationsEnabled'),
      animationAutoSwitch: _boolean(settings['animationsAutoSwitch'], 'settings.animationsAutoSwitch'),
      animationsOnAc: _boolean(settings['animationsOnAC'], 'settings.animationsOnAC'),
      animationsOnBattery: _boolean(settings['animationsOnBattery'], 'settings.animationsOnBattery'),
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
    if (value is num && value >= 0 && value == value.roundToDouble()) return value.toInt();
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
  };
}

abstract interface class PowerProfileService {
  Future<ProfileState> load();
  Future<ProfileState> apply(ProfileState state);
}

class PowerProfileConnectionException implements Exception {
  PowerProfileConnectionException(this.message);
  final String message;
  @override
  String toString() => message;
}

/// Windows client. The native runner owns the Win32 named-pipe call because
/// Dart's standard library does not expose named-pipe handles on Windows.
class WindowsPowerProfileService implements PowerProfileService {
  static const _channel = MethodChannel('powerprofile/local_pipe');

  @override
  Future<ProfileState> load() => _request('getState').then(ProfileState.fromJson);

  @override
  Future<ProfileState> apply(ProfileState state) =>
      _request('apply', state.toApplyJson()).then(ProfileState.fromJson);

  Future<Map<String, dynamic>> _request(String command, [Map<String, dynamic>? payload]) async {
    if (!Platform.isWindows) {
      throw PowerProfileConnectionException('The Windows backend is unavailable on this platform.');
    }

    final requestPayload = <String, dynamic>{
      'id': DateTime.now().microsecondsSinceEpoch.toString(),
      'version': 1,
      'command': command,
    };
    if (payload != null) {
      requestPayload['payload'] = payload;
    }
    final request = jsonEncode(requestPayload);
    try {
      final raw = await _channel.invokeMethod<String>('request', request);
      if (raw == null) throw PowerProfileConnectionException('The native pipe returned no response.');
      final decoded = jsonDecode(raw);
      if (decoded is! Map) throw const FormatException('The native pipe returned an invalid response.');
      final response = Map<String, dynamic>.from(decoded);
      if (response['ok'] != true) {
        final error = response['error'];
        final message = error is Map ? error['message'] : null;
        throw PowerProfileConnectionException(message is String ? message : 'The backend rejected the request.');
      }
      final data = response['data'];
      if (data is! Map) throw const FormatException('The native pipe response has no state data.');
      return Map<String, dynamic>.from(data);
    } on PlatformException catch (error) {
      throw PowerProfileConnectionException(error.message ?? 'Unable to connect to the PowerProfile host.');
    } on FormatException catch (error) {
      throw PowerProfileConnectionException(error.message);
    }
  }
}

/// Only for widget tests and explicit non-Windows preview use.
class MockPowerProfileService implements PowerProfileService {
  ProfileState _state = const ProfileState(
    powerSource: 'AC', currentRefreshRate: 120, availableRefreshRates: [60, 90, 120, 144],
    refreshRateEnabled: true, refreshRateAutoSwitch: true, refreshRateAc: 120, refreshRateBattery: 60,
    animationEnabled: true, animationAutoSwitch: true, animationsOnAc: true, animationsOnBattery: false,
  );

  @override
  Future<ProfileState> load() async => _state;
  @override
  Future<ProfileState> apply(ProfileState state) async => _state = state;
}
