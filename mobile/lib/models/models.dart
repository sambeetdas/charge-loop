class Station {
  final String id;
  final String name;
  final String address;
  final String city;
  final double latitude;
  final double longitude;
  final int availableCharged;
  final int charging;
  final int waitTimeMinutes;
  final double distanceKm;

  Station({
    required this.id,
    required this.name,
    required this.address,
    required this.city,
    required this.latitude,
    required this.longitude,
    required this.availableCharged,
    required this.charging,
    required this.waitTimeMinutes,
    required this.distanceKm,
  });

  factory Station.fromJson(Map<String, dynamic> json) {
    return Station(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      address: json['address'] ?? '',
      city: json['city'] ?? '',
      latitude: (json['latitude'] as num?)?.toDouble() ?? 0.0,
      longitude: (json['longitude'] as num?)?.toDouble() ?? 0.0,
      availableCharged: json['availableChargedBatteries'] ?? 0,
      charging: json['chargingBatteries'] ?? 0,
      waitTimeMinutes: json['estimatedWaitMinutes'] ?? 0,
      distanceKm: (json['distanceKm'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class DriverProfile {
  final String id;
  final String phoneNumber;
  final String fullName;
  final String token;
  final int kycStatus;
  final double walletBalance;

  DriverProfile({
    required this.id,
    required this.phoneNumber,
    required this.fullName,
    required this.token,
    required this.kycStatus,
    required this.walletBalance,
  });

  factory DriverProfile.fromJson(Map<String, dynamic> json) {
    return DriverProfile(
      id: json['driverId'] ?? '',
      phoneNumber: json['phoneNumber'] ?? '',
      fullName: json['fullName'] ?? 'Ravi Sharma',
      token: json['token'] ?? '',
      kycStatus: json['kycStatus'] ?? 1,
      walletBalance: (json['walletBalance'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class SwapRecord {
  final String swapId;
  final int status;
  final String depletedSerial;
  final int depletedSoc;
  final String issuedSerial;
  final int issuedSoc;
  final double totalAmount;
  final String message;
  final int? durationSeconds;

  SwapRecord({
    required this.swapId,
    required this.status,
    required this.depletedSerial,
    required this.depletedSoc,
    required this.issuedSerial,
    required this.issuedSoc,
    required this.totalAmount,
    required this.message,
    this.durationSeconds,
  });

  factory SwapRecord.fromJson(Map<String, dynamic> json) {
    return SwapRecord(
      swapId: json['swapId'] ?? '',
      status: json['status'] ?? 0,
      depletedSerial: json['depletedBatterySerial'] ?? '',
      depletedSoc: json['depletedBatterySoc'] ?? 0,
      issuedSerial: json['issuedBatterySerial'] ?? '',
      issuedSoc: json['issuedBatterySoc'] ?? 100,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 118.0,
      message: json['message'] ?? '',
      durationSeconds: json['durationSeconds'],
    );
  }
}
