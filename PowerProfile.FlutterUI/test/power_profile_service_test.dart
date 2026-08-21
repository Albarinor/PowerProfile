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

  test('ProfileState serializes complete independent animation policies', () {
    final updated = ProfileState.fromJson(stateJson).copyWith(
      refreshRateBattery: 120,
      animationAutoSwitch: false,
      animationPolicyAc: const AnimationPolicy(taskbarAnimations: false),
      animationPolicyBattery: const AnimationPolicy(
        taskbarAnimations: true,
        tooltipFade: false,
      ),
      uiLanguage: 'zh',
    );

    final payload = updated.toApplyJson();
    expect(payload['refreshRateBattery'], 120);
    expect(payload['animationsAutoSwitch'], isFalse);
    expect(payload['uiLanguage'], 'zh');
    expect(payload['animationPolicyAC'], {
      'windowAnimation': true,
      'taskbarAnimations': false,
      'menuAnimation': true,
      'comboBoxAnimation': true,
      'listBoxSmoothScrolling': true,
      'selectionFade': true,
      'tooltipAnimation': true,
      'tooltipFade': true,
      'cursorShadow': true,
      'uiEffects': true,
      'clientAreaAnimation': true,
      'disableOverlappedContent': false,
    });
    expect(payload['animationPolicyBattery'], {
      'windowAnimation': true,
      'taskbarAnimations': true,
      'menuAnimation': true,
      'comboBoxAnimation': true,
      'listBoxSmoothScrolling': true,
      'selectionFade': true,
      'tooltipAnimation': true,
      'tooltipFade': false,
      'cursorShadow': true,
      'uiEffects': true,
      'clientAreaAnimation': true,
      'disableOverlappedContent': false,
    });
  });

  test('ProfileState reads the persisted UI language when supplied', () {
    final state = ProfileState.fromJson({
      ...stateJson,
      'settings': {...stateJson['settings'] as Map, 'uiLanguage': 'zh'},
    });

    expect(state.uiLanguage, 'zh');
  });

  test('ProfileState rejects an invalid backend response', () {
    final invalid = Map<String, dynamic>.from(stateJson)
      ..['availableRefreshRates'] = 'not a list';
    expect(() => ProfileState.fromJson(invalid), throwsFormatException);
  });
}
