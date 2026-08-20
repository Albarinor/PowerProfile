import 'package:flutter_test/flutter_test.dart';
import 'package:powerprofile_flutter_ui/power_profile_service.dart';

void main() {
  const stateJson = <String, dynamic>{
    'powerSource': 'AC',
    'currentRefreshRate': 120,
    'availableRefreshRates': [144, 60, 120, 120],
    'settings': {
      'refreshRateEnabled': true,
      'refreshRateAutoSwitch': false,
      'refreshRateAC': 144,
      'refreshRateBattery': 60,
      'animationsEnabled': true,
      'animationsAutoSwitch': true,
      'animationsOnAC': true,
      'animationsOnBattery': false,
    },
  };

  test('ProfileState parses backend JSON and normalizes refresh rates', () {
    final state = ProfileState.fromJson(stateJson);

    expect(state.powerSource, 'AC');
    expect(state.availableRefreshRates, [60, 120, 144]);
    expect(state.refreshRateAc, 144);
    expect(state.animationsOnBattery, isFalse);
  });

  test(
    'ProfileState copyWith retains native fields and serializes apply payload',
    () {
      final updated = ProfileState.fromJson(stateJson).copyWith(
        refreshRateBattery: 120,
        animationAutoSwitch: false,
        animationsOnBattery: true,
      );

      expect(updated.currentRefreshRate, 120);
      expect(updated.availableRefreshRates, [60, 120, 144]);
      expect(updated.toApplyJson(), {
        'refreshRateEnabled': true,
        'refreshRateAutoSwitch': false,
        'refreshRateAC': 144,
        'refreshRateBattery': 120,
        'animationsEnabled': true,
        'animationsAutoSwitch': false,
        'animationsOnAC': true,
        'animationsOnBattery': true,
      });
    },
  );

  test('ProfileState rejects an invalid backend response', () {
    final invalid = Map<String, dynamic>.from(stateJson)
      ..['availableRefreshRates'] = 'not a list';
    expect(() => ProfileState.fromJson(invalid), throwsFormatException);
  });
}
