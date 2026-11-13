package com.chargeloop.driver.data.model

import com.google.gson.annotations.SerializedName

data class StationDto(
    @SerializedName("id") val id: String,
    @SerializedName("name") val name: String,
    @SerializedName("address") val address: String,
    @SerializedName("city") val city: String,
    @SerializedName("latitude") val latitude: Double,
    @SerializedName("longitude") val longitude: Double,
    @SerializedName("availableChargedBatteries") val availableCharged: Int,
    @SerializedName("chargingBatteries") val charging: Int,
    @SerializedName("estimatedWaitMinutes") val waitTimeMinutes: Int,
    @SerializedName("distanceKm") val distanceKm: Double?
)

data class NearbyStationsRequest(
    @SerializedName("latitude") val latitude: Double,
    @SerializedName("longitude") val longitude: Double,
    @SerializedName("radiusKm") val radiusKm: Double = 15.0
)

data class InitiateSwapRequest(
    @SerializedName("driverId") val driverId: String,
    @SerializedName("stationId") val stationId: String,
    @SerializedName("partnerId") val partnerId: String,
    @SerializedName("depletedBatterySerial") val depletedBatterySerial: String,
    @SerializedName("issuedBatterySerial") val issuedBatterySerial: String
)

data class ConfirmSwapRequest(
    @SerializedName("swapId") val swapId: String,
    @SerializedName("paymentMethod") val paymentMethod: Int = 1
)

data class SwapResponse(
    @SerializedName("swapId") val swapId: String,
    @SerializedName("status") val status: Int,
    @SerializedName("depletedBatterySerial") val depletedBatterySerial: String,
    @SerializedName("depletedBatterySoc") val depletedBatterySoc: Int,
    @SerializedName("issuedBatterySerial") val issuedBatterySerial: String,
    @SerializedName("issuedBatterySoc") val issuedBatterySoc: Int,
    @SerializedName("totalAmount") val totalAmount: Double,
    @SerializedName("partnerCommission") val partnerCommission: Double,
    @SerializedName("durationSeconds") val durationSeconds: Int?,
    @SerializedName("message") val message: String
)

data class WalletTopUpRequest(
    @SerializedName("accountId") val accountId: String,
    @SerializedName("accountType") val accountType: Int = 0,
    @SerializedName("amount") val amount: Double,
    @SerializedName("paymentReference") val paymentReference: String
)

data class WalletBalanceResponse(
    @SerializedName("accountId") val accountId: String,
    @SerializedName("balance") val balance: Double
)
