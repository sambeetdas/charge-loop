class Station {
  final String id;
  final String partnerId;
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
    required this.partnerId,
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
      partnerId: json['partnerId'] ?? '',
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
  final String? referralCode;
  final String? email;

  DriverProfile({
    required this.id,
    required this.phoneNumber,
    required this.fullName,
    required this.token,
    required this.kycStatus,
    required this.walletBalance,
    this.referralCode,
    this.email,
  });

  factory DriverProfile.fromJson(Map<String, dynamic> json) {
    return DriverProfile(
      id: json['driverId'] ?? json['id'] ?? '',
      phoneNumber: json['phoneNumber'] ?? '',
      fullName: json['fullName'] ?? 'Driver',
      token: json['token'] ?? '',
      kycStatus: json['kycStatus'] ?? 0,
      walletBalance: (json['walletBalance'] as num?)?.toDouble() ?? 0.0,
      referralCode: json['referralCode'],
      email: json['email'],
    );
  }
}

class VendorProfile {
  final String id;
  final String phoneNumber;
  final String fullName;
  final String businessName;
  final String token;
  final double walletBalance;
  final String? upiId;
  final String? bankAccountNumber;

  VendorProfile({
    required this.id,
    required this.phoneNumber,
    required this.fullName,
    required this.businessName,
    required this.token,
    required this.walletBalance,
    this.upiId,
    this.bankAccountNumber,
  });

  factory VendorProfile.fromJson(Map<String, dynamic> json) {
    return VendorProfile(
      id: json['vendorId'] ?? json['id'] ?? '',
      phoneNumber: json['phoneNumber'] ?? '',
      fullName: json['fullName'] ?? 'Vendor',
      businessName: json['businessName'] ?? 'Station Partner',
      token: json['token'] ?? '',
      walletBalance: (json['walletBalance'] as num?)?.toDouble() ?? 0.0,
      upiId: json['upiId'],
      bankAccountNumber: json['bankAccountNumber'],
    );
  }
}

class VehicleModel {
  final String id;
  final String registrationNumber;
  final String oem;
  final String model;
  final String batteryCompatibilityType;

  VehicleModel({
    required this.id,
    required this.registrationNumber,
    required this.oem,
    required this.model,
    required this.batteryCompatibilityType,
  });

  factory VehicleModel.fromJson(Map<String, dynamic> json) {
    return VehicleModel(
      id: json['id'] ?? '',
      registrationNumber: json['registrationNumber'] ?? '',
      oem: json['oem'] ?? '',
      model: json['model'] ?? '',
      batteryCompatibilityType: json['batteryCompatibilityType'] ?? '2W_48V',
    );
  }
}

class BatteryModel {
  final String id;
  final String serialNumber;
  final String modelType;
  final int stateOfCharge;
  final int stateOfHealth;
  final double temperature;
  final int status;

  BatteryModel({
    required this.id,
    required this.serialNumber,
    required this.modelType,
    required this.stateOfCharge,
    required this.stateOfHealth,
    required this.temperature,
    required this.status,
  });

  factory BatteryModel.fromJson(Map<String, dynamic> json) {
    return BatteryModel(
      id: json['id'] ?? '',
      serialNumber: json['serialNumber'] ?? '',
      modelType: json['modelType'] ?? '2W_48V',
      stateOfCharge: json['stateOfChargePercent'] ?? json['stateOfCharge'] ?? 100,
      stateOfHealth: json['stateOfHealthPercent'] ?? json['stateOfHealth'] ?? 100,
      temperature: (json['cellTemperatureCelsius'] as num?)?.toDouble() ?? 30.0,
      status: json['status'] ?? 0,
    );
  }
}

class DriverDetailModel {
  final String id;
  final String phoneNumber;
  final String fullName;
  final String languageCode;
  final int kycStatus;
  final String? drivingLicenseNumber;
  final String? aadhaarMasked;
  final double walletBalance;
  final List<VehicleModel> vehicles;
  final BatteryModel? activeBattery;
  final String? email;
  final String? referralCode;

  DriverDetailModel({
    required this.id,
    required this.phoneNumber,
    required this.fullName,
    required this.languageCode,
    required this.kycStatus,
    this.drivingLicenseNumber,
    this.aadhaarMasked,
    required this.walletBalance,
    required this.vehicles,
    this.activeBattery,
    this.email,
    this.referralCode,
  });

  factory DriverDetailModel.fromJson(Map<String, dynamic> json) {
    var vehiclesList = <VehicleModel>[];
    if (json['vehicles'] != null) {
      vehiclesList = (json['vehicles'] as List)
          .map((v) => VehicleModel.fromJson(v))
          .toList();
    }
    return DriverDetailModel(
      id: json['id'] ?? '',
      phoneNumber: json['phoneNumber'] ?? '',
      fullName: json['fullName'] ?? 'Driver',
      languageCode: json['languageCode'] ?? 'en',
      kycStatus: json['kycStatus'] ?? 0,
      drivingLicenseNumber: json['drivingLicenseNumber'],
      aadhaarMasked: json['aadhaarMasked'],
      walletBalance: (json['walletBalance'] as num?)?.toDouble() ?? 0.0,
      vehicles: vehiclesList,
      activeBattery: json['activeBattery'] != null
          ? BatteryModel.fromJson(json['activeBattery'])
          : null,
      email: json['email'],
      referralCode: json['referralCode'],
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
