import 'environment.dart';

class ApiConstants {
  /// Base URL from environment configuration
  /// 
  /// This uses Environment.apiBaseUrl which can be configured via dart-define
  /// Default: http://10.0.2.2:5220/api (for Android AVD emulator)
  static String get baseUrl => Environment.apiBaseUrl;

  // ==================== AUTH ENDPOINTS ====================
  static const String login = '/Auth/login';
  static const String register = '/Auth/register';
  static const String validateToken = '/Auth/validate';
  static const String me = '/Auth/me';
  static const String changePassword = '/Auth/change-password';
  static const String refresh = '/Auth/refresh';
  static const String logout = '/Auth/logout';

  // ==================== PRODUCTS & CATEGORIES ====================
  static const String products = '/Products';
  static const String categories = '/Categories';
  
  // ==================== ORDERS ====================
  static const String orders = '/Orders';
  static const String activeOrders = '/Orders/active';
  
  // ==================== TABLES & USERS ====================
  static const String tables = '/Tables';
  static const String users = '/Users';

  // ==================== INVENTORY ====================
  static const String inventory = '/Inventory/store-products';
  static const String lowStock = '/Inventory/low-stock';

  // ==================== PROCUREMENT ====================
  static const String procurement = '/Procurement';
  static const String procurementPaymentIntent = '/Procurement/{id}/payment-intent';
  static const String procurementConfirmPayment = '/Procurement/{id}/confirm-payment';

  // ==================== STATISTICS ====================
  static const String statistics = '/Statistics/dashboard';
  static const String dailyStats = '/Statistics/daily';

  // ==================== NOTIFICATIONS & RECEIPTS ====================
  static const String notifications = '/Notifications';
  static const String receipts = '/Receipts/customer';
  static const String kitchenReceipts = '/Receipts/kitchen';
  static const String barReceipts = '/Receipts/bar';

  // ==================== RECOMMENDATIONS ====================
  static const String recommendations = '/Recommendations';
  static const String popularProducts = '/Recommendations/popular';

  // ==================== TIMEOUT ====================
  static Duration get timeout => Environment.timeout;

  /// Print API configuration (for debugging)
  static void printConfig() {
    print('🌐 API Configuration:');
    print('   Base URL: $baseUrl');
    print('   Timeout: ${timeout.inSeconds}s');
  }
}
