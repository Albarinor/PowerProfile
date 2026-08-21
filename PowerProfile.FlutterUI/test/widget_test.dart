import 'package:flutter/material.dart' show Key;
import 'package:flutter_test/flutter_test.dart';
import 'package:powerprofile_flutter_ui/main.dart';
import 'package:powerprofile_flutter_ui/power_profile_service.dart';

class FixedPowerProfileService implements PowerProfileService {
  FixedPowerProfileService(this.state);
  final ProfileState state;

  @override
  Future<ProfileState> apply(ProfileState value) async => value;

  @override
  Future<ProfileState> load() async => state;

  @override
  Future<ProfileState> setLanguage(String language) async =>
      state.copyWith(uiLanguage: language);
}

void main() {
  testWidgets('renders AC and battery refresh-rate controls', (tester) async {
    await tester.pumpWidget(
      PowerProfileApp(
        service: FixedPowerProfileService(
          const ProfileState(
            powerSource: 'Battery',
            currentRefreshRate: 60,
            availableRefreshRates: [60, 120],
            refreshRateAc: 120,
            refreshRateBattery: 60,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('AC target rate')), findsOneWidget);
    expect(find.byKey(const Key('Battery target rate')), findsOneWidget);
    expect(find.text('Auto-switch by power source'), findsNWidgets(2));
    expect(find.text('Animations on AC'), findsOneWidget);
    expect(find.text('Animations on battery'), findsOneWidget);
  });

  testWidgets(
    'handles no available refresh rates without an invalid dropdown value',
    (tester) async {
      await tester.pumpWidget(
        PowerProfileApp(
          service: FixedPowerProfileService(
            const ProfileState(
              availableRefreshRates: [],
              refreshRateAc: 144,
              refreshRateBattery: 60,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('No refresh rates reported'), findsNWidgets(2));
    },
  );
}
