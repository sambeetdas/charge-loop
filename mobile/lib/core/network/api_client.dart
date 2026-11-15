import 'dart:convert';
import 'dart:math';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../../models/models.dart';

class ApiClient {
  final String baseUrl;
  static String? authToken;

  ApiClient({this.baseUrl = ApiConstants.localhostUrl});

  static String newCorrelationId() {
    final rng = Random();
    String hex(int count) => List.generate(count, (_) => rng.nextInt(16).toRadixString(16)).join();
    return '${hex(8)}-${hex(4)}-4${hex(3)}-${['8', '9', 'a', 'b'][rng.nextInt(4)]}${hex(3)}-${hex(12)}';
  }

  Map<String, String> _headers() {
    return {
      'Content-Type': 'application/json',
      'X-Correlation-Id': newCorrelationId(),
      if (authToken != null && authToken!.isNotEmpty) 'Authorization': 'Bearer $authToken',
    };
  }

  // 1. Driver Authentication (Reads and registers directly in PostgreSQL)
  Future<DriverProfile> authenticate(String phone, {String? name}) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.authPhone}'),
      headers: _headers(),
      body: jsonEncode({
        'phoneNumber': phone.trim(),
        'fullName': name?.trim(),
        'languageCode': 'en',
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final profile = DriverProfile.fromJson(jsonDecode(response.body));
      authToken = profile.token;
      return profile;
    } else {
      throw Exception('Authentication failed: ${response.body}');
    }
  }

  // 1a. OTP Send & Verify
  Future<Map<String, dynamic>> sendOtp(String phone) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/send-otp'),
      headers: _headers(),
      body: jsonEncode({'phoneNumber': phone.trim()}),
    ).timeout(const Duration(seconds: 8));
    if (response.statusCode == 200) return jsonDecode(response.body);
    throw Exception('Failed to send OTP: ${response.body}');
  }

  Future<DriverProfile> verifyOtp({
    required String phone,
    required String otpCode,
    String? fullName,
    String? email,
    String? referralCode,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/verify-otp'),
      headers: _headers(),
      body: jsonEncode({
        'phoneNumber': phone.trim(),
        'otpCode': otpCode.trim(),
        'fullName': fullName?.trim(),
        'email': email?.trim(),
        'referralCode': referralCode?.trim(),
      }),
    ).timeout(const Duration(seconds: 8));
    if (response.statusCode == 200) {
      final profile = DriverProfile.fromJson(jsonDecode(response.body));
      authToken = profile.token;
      return profile;
    }
    throw Exception('OTP verification failed: ${response.body}');
  }

  Future<Map<String, dynamic>> claimReferral(String driverId, String referralCode) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/referral/claim'),
      headers: _headers(),
      body: jsonEncode({'driverId': driverId, 'referralCode': referralCode.trim()}),
    ).timeout(const Duration(seconds: 8));
    if (response.statusCode == 200) return jsonDecode(response.body);
    throw Exception('Referral claim failed: ${response.body}');
  }

  Future<Map<String, dynamic>> validateDriverForSwap(String query) async {
    final response = await http.get(
      Uri.parse('$baseUrl/swaps/validate-driver?query=${Uri.encodeComponent(query.trim())}'),
      headers: _headers(),
    ).timeout(const Duration(seconds: 8));
    if (response.statusCode == 200) return jsonDecode(response.body);
    throw Exception('Driver validation failed: ${response.body}');
  }

  Future<BatteryModel> registerBatteryAtStation({
    required String stationId,
    required String serialNumber,
    String modelType = 'LFP-48V-30Ah',
    int soc = 100,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/stations/$stationId/batteries'),
      headers: _headers(),
      body: jsonEncode({
        'stationId': stationId,
        'serialNumber': serialNumber.trim(),
        'modelType': modelType.trim(),
        'stateOfChargePercent': soc,
      }),
    ).timeout(const Duration(seconds: 8));
    if (response.statusCode == 200) return BatteryModel.fromJson(jsonDecode(response.body));
    throw Exception('Battery registration failed: ${response.body}');
  }

  Future<Map<String, dynamic>> publishSwapBill(String swapId, List<String> channels) async {
    final response = await http.post(
      Uri.parse('$baseUrl/swaps/$swapId/publish-bill'),
      headers: _headers(),
      body: jsonEncode({'channels': channels}),
    ).timeout(const Duration(seconds: 8));
    if (response.statusCode == 200) return jsonDecode(response.body);
    throw Exception('Bill publish failed: ${response.body}');
  }

  // 1b. Vendor Authentication
  Future<VendorProfile> authenticateVendor(String phone, {String? name, String? businessName}) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/vendor/phone'),
      headers: _headers(),
      body: jsonEncode({
        'phoneNumber': phone.trim(),
        'fullName': name?.trim(),
        'businessName': businessName?.trim(),
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final vendor = VendorProfile.fromJson(jsonDecode(response.body));
      authToken = vendor.token;
      return vendor;
    } else {
      throw Exception('Vendor authentication failed: ${response.body}');
    }
  }

  // 2. Fetch Driver Details (Vehicles, active battery, balance directly from PostgreSQL)
  Future<DriverDetailModel> getDriverDetails(String driverId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/auth/profile/$driverId'),
      headers: _headers(),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      return DriverDetailModel.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to load driver profile: ${response.body}');
    }
  }

  // 3. Fetch Nearby Stations directly from PostgreSQL
  Future<List<Station>> getNearbyStations({
    double lat = 28.6289,
    double lng = 77.0818,
    double radiusKm = 15.0,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.stationsNearby}'),
      headers: _headers(),
      body: jsonEncode({
        'latitude': lat,
        'longitude': lng,
        'radiusKm': radiusKm,
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final List data = jsonDecode(response.body);
      return data.map((json) => Station.fromJson(json)).toList();
    } else {
      throw Exception('Failed to fetch stations from database: ${response.body}');
    }
  }

  // 4. Fetch Ready Charged Batteries at a Station directly from PostgreSQL
  Future<List<BatteryModel>> getStationBatteries(String stationId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/stations/$stationId/batteries'),
      headers: _headers(),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final List data = jsonDecode(response.body);
      return data.map((json) => BatteryModel.fromJson(json)).toList();
    } else {
      throw Exception('Failed to fetch station batteries: ${response.body}');
    }
  }

  // 5. Initiate Battery Swap (Records in PostgreSQL)
  Future<SwapResponseModel> initiateSwap({
    required String driverId,
    required String stationId,
    required String partnerId,
    required String depletedBatterySerial,
    required String issuedBatterySerial,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.swapInitiate}'),
      headers: _headers(),
      body: jsonEncode({
        'driverId': driverId,
        'stationId': stationId,
        'partnerId': partnerId,
        'depletedBatterySerial': depletedBatterySerial,
        'issuedBatterySerial': issuedBatterySerial,
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      return SwapResponseModel.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Swap initiation failed: ${response.body}');
    }
  }

  // 6. Confirm Swap Completion (Updates PostgreSQL ledger and battery states)
  Future<SwapRecord> confirmSwap({
    required String swapId,
    int paymentMethod = 3, // Cash
    double? cashAmount,
    String? correlationId,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.swapConfirm}'),
      headers: _headers(),
      body: jsonEncode({
        'swapId': swapId,
        'paymentMethod': paymentMethod,
        if (cashAmount != null) 'cashAmount': cashAmount,
        'correlationId': correlationId ?? newCorrelationId(),
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      return SwapRecord.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Swap confirmation failed: ${response.body}');
    }
  }

  // 7. Wallet Top Up (Updates PostgreSQL driver balance and transaction history)
  Future<double> topUpWallet(String accountId, double amount, {String paymentMethod = 'CASH', String? correlationId}) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.walletTopUp}'),
      headers: _headers(),
      body: jsonEncode({
        'accountId': accountId,
        'accountType': 0, // Driver
        'amount': amount,
        'paymentReference': '$paymentMethod-REF-${DateTime.now().millisecondsSinceEpoch}',
        'correlationId': correlationId ?? newCorrelationId(),
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return (data['balance'] as num).toDouble();
    } else {
      throw Exception('Wallet top-up failed: ${response.body}');
    }
  }

  // 8. Create & Register Station (Vendor Hub)
  Future<StationModel> createStation({
    required String partnerId,
    required String name,
    required String address,
    required String city,
    double latitude = 28.6289,
    double longitude = 77.0818,
    int totalBays = 12,
    int initialCharged = 6,
    int initialCharging = 4,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/v1/stations'),
      headers: _headers(),
      body: jsonEncode({
        'partnerId': partnerId,
        'name': name,
        'address': address,
        'city': city,
        'latitude': latitude,
        'longitude': longitude,
        'totalBays': totalBays,
        'initialChargedBatteries': initialCharged,
        'initialChargingBatteries': initialCharging,
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200 || response.statusCode == 201) {
      return StationModel.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to register station: ${response.body}');
    }
  }

  // 9. Fetch Vendor Stations
  Future<List<StationModel>> getVendorStations(String partnerId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/v1/stations/partner/$partnerId'),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final List list = jsonDecode(response.body);
      return list.map((item) => StationModel.fromJson(item)).toList();
    } else {
      return [];
    }
  }
}

class SwapResponseModel {
  final String swapId;
  final int status;
  final String depletedSerial;
  final int depletedSoc;
  final String issuedSerial;
  final int issuedSoc;
  final double totalAmount;
  final String message;

  SwapResponseModel({
    required this.swapId,
    required this.status,
    required this.depletedSerial,
    required this.depletedSoc,
    required this.issuedSerial,
    required this.issuedSoc,
    required this.totalAmount,
    required this.message,
  });

  factory SwapResponseModel.fromJson(Map<String, dynamic> json) {
    return SwapResponseModel(
      swapId: json['swapId'] ?? '',
      status: json['status'] ?? 0,
      depletedSerial: json['depletedBatterySerial'] ?? '',
      depletedSoc: json['depletedBatterySoc'] ?? 0,
      issuedSerial: json['issuedBatterySerial'] ?? '',
      issuedSoc: json['issuedBatterySoc'] ?? 100,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 118.0,
      message: json['message'] ?? '',
    );
  }
}
