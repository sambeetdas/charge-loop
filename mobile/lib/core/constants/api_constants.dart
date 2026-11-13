class ApiConstants {
  // Use 10.0.2.2 for Android Emulator, or localhost for macOS / Desktop
  static const String baseUrl = 'http://10.0.2.2:5000/api/v1';
  static const String localhostUrl = 'http://localhost:5000/api/v1';

  static const String authPhone = '/auth/phone';
  static const String authKyc = '/auth/kyc';
  static const String authVehicle = '/auth/vehicle';
  static const String stationsNearby = '/stations/nearby';
  static const String swapInitiate = '/swaps/initiate';
  static const String swapConfirm = '/swaps/confirm';
  static const String swapHistory = '/swaps/history';
  static const String wallet = '/wallet';
  static const String walletTopUp = '/wallet/topup';
}
