import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../../models/models.dart';

class ApiClient {
  final String baseUrl;

  ApiClient({this.baseUrl = ApiConstants.localhostUrl});

  // 1. Driver Authentication (Reads and registers directly in PostgreSQL)
  Future<DriverProfile> authenticate(String phone, {String? name}) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.authPhone}'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'phoneNumber': phone.trim(),
        'fullName': name?.trim(),
        'languageCode': 'hi',
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      return DriverProfile.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Authentication failed: ${response.body}');
    }
  }

  // 1b. Vendor Authentication
  Future<VendorProfile> authenticateVendor(String phone, {String? name, String? businessName}) async {
    final response = await http.post(
      Uri.parse('$baseUrl/auth/vendor/phone'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'phoneNumber': phone.trim(),
        'fullName': name?.trim(),
        'businessName': businessName?.trim(),
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      return VendorProfile.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Vendor authentication failed: ${response.body}');
    }
  }

  // 2. Fetch Driver Details (Vehicles, active battery, balance directly from PostgreSQL)
  Future<DriverDetailModel> getDriverDetails(String driverId) async {
    final response = await http.get(
      Uri.parse('$baseUrl/auth/profile/$driverId'),
      headers: {'Content-Type': 'application/json'},
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
      headers: {'Content-Type': 'application/json'},
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
      headers: {'Content-Type': 'application/json'},
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      final List data = jsonDecode(response.body);
      return data.map((json) => BatteryModel.fromJson(json)).toList();
    } else {
      throw Exception('Failed to fetch station batteries: ${response.body}');
    }
  }

  // 5. Initiate 2-Minute Swap (Records in PostgreSQL)
  Future<SwapResponseModel> initiateSwap({
    required String driverId,
    required String stationId,
    required String partnerId,
    required String depletedBatterySerial,
    required String issuedBatterySerial,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.swapInitiate}'),
      headers: {'Content-Type': 'application/json'},
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
    int paymentMethod = 1, // Wallet
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.swapConfirm}'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'swapId': swapId,
        'paymentMethod': paymentMethod,
      }),
    ).timeout(const Duration(seconds: 8));

    if (response.statusCode == 200) {
      return SwapRecord.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Swap confirmation failed: ${response.body}');
    }
  }

  // 7. Wallet Top Up (Updates PostgreSQL driver balance and transaction history)
  Future<double> topUpWallet(String accountId, double amount) async {
    final response = await http.post(
      Uri.parse('$baseUrl${ApiConstants.walletTopUp}'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'accountId': accountId,
        'accountType': 0, // Driver
        'amount': amount,
        'paymentReference': 'UPI-REF-${DateTime.now().millisecondsSinceEpoch}',
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
      headers: {'Content-Type': 'application/json'},
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
