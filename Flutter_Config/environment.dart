/// Environment configuration for API endpoints
/// 
/// Usage:
/// - Default build (AVD emulator): uses 10.0.2.2
/// - Custom build: flutter build apk --dart-define=API_BASE_URL=http://your-ip:5220/api
class Environment {
  /// API Base URL
  /// 
  /// Default: http://10.0.2.2:5220/api (Android AVD emulator)
  /// 
  /// For production or custom environments, pass via dart-define:
  /// ```
  /// flutter run --dart-define=API_BASE_URL=http://192.168.1.100:5220/api
  /// flutter build apk --dart-define=API_BASE_URL=http://10.0.2.2:5220/api
  /// ```
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5220/api', // Default za AVD emulator
  );

  /// Timeout duration for HTTP requests
  static const Duration timeout = Duration(seconds: 60);

  /// Enable debug logging
  static const bool enableLogging = true;

  /// Print current configuration
  static void printConfig() {
    print('🔧 Environment Configuration:');
    print('   API Base URL: $apiBaseUrl');
    print('   Timeout: ${timeout.inSeconds}s');
    print('   Logging: $enableLogging');
  }
}
