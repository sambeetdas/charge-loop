package com.chargeloop.driver.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.chargeloop.driver.data.api.ChargeLoopApiService
import com.chargeloop.driver.data.model.*
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

enum class SwapStep {
    IDLE, VALIDATING, IN_PROGRESS, COMPLETED
}

class MainViewModel : ViewModel() {
    private val api = ChargeLoopApiService.create()

    private val _stations = MutableStateFlow<List<StationDto>>(emptyList())
    val stations: StateFlow<List<StationDto>> = _stations.asStateFlow()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private val _walletBalance = MutableStateFlow(450.0)
    val walletBalance: StateFlow<Double> = _walletBalance.asStateFlow()

    private val _batterySoc = MutableStateFlow(16)
    val batterySoc: StateFlow<Int> = _batterySoc.asStateFlow()

    private val _swapStep = MutableStateFlow(SwapStep.IDLE)
    val swapStep: StateFlow<SwapStep> = _swapStep.asStateFlow()

    private val _timerSeconds = MutableStateFlow(0)
    val timerSeconds: StateFlow<Int> = _timerSeconds.asStateFlow()

    private val _selectedLanguage = MutableStateFlow("hi")
    val selectedLanguage: StateFlow<String> = _selectedLanguage.asStateFlow()

    private var timerJob: Job? = null
    private var activeSwapId: String? = null

    init {
        loadStations()
    }

    fun setLanguage(lang: String) {
        _selectedLanguage.value = lang
    }

    fun loadStations() {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                val list = api.getNearbyStations(NearbyStationsRequest(28.6289, 77.0818, 15.0))
                _stations.value = list
            } catch (e: Exception) {
                // Fallback seed data if running disconnected
                _stations.value = listOf(
                    StationDto(
                        id = "aaaa1111-aaaa-1111-aaaa-111111111111",
                        name = "ChargeLoop Janakpuri Hub",
                        address = "District Centre, Janakpuri, West Delhi",
                        city = "New Delhi",
                        latitude = 28.6289,
                        longitude = 77.0818,
                        availableCharged = 8,
                        charging = 6,
                        waitTimeMinutes = 2,
                        distanceKm = 0.8
                    ),
                    StationDto(
                        id = "bbbb2222-bbbb-2222-bbbb-222222222222",
                        name = "ChargeLoop Connaught Place Central",
                        address = "Outer Circle, Block L, Connaught Place",
                        city = "New Delhi",
                        latitude = 28.6327,
                        longitude = 77.2195,
                        availableCharged = 12,
                        charging = 5,
                        waitTimeMinutes = 0,
                        distanceKm = 4.2
                    )
                )
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun startSwap() {
        viewModelScope.launch {
            _swapStep.value = SwapStep.VALIDATING
            try {
                val res = api.initiateSwap(
                    InitiateSwapRequest(
                        driverId = "99999999-9999-9999-9999-999999999999",
                        stationId = "aaaa1111-aaaa-1111-aaaa-111111111111",
                        partnerId = "11111111-1111-1111-1111-111111111111",
                        depletedBatterySerial = "BAT-48V-9999",
                        issuedBatterySerial = "BAT-48V-1001"
                    )
                )
                activeSwapId = res.swapId
            } catch (_: Exception) {}

            _swapStep.value = SwapStep.IN_PROGRESS
            _timerSeconds.value = 0
            timerJob?.cancel()
            timerJob = viewModelScope.launch {
                while (true) {
                    delay(1000)
                    _timerSeconds.value += 1
                }
            }
        }
    }

    fun completeSwap() {
        timerJob?.cancel()
        viewModelScope.launch {
            if (activeSwapId != null) {
                try {
                    api.confirmSwap(ConfirmSwapRequest(activeSwapId!!, 1))
                } catch (_: Exception) {}
            }
            _walletBalance.value -= 118.0
            _batterySoc.value = 100
            _swapStep.value = SwapStep.COMPLETED
        }
    }

    fun resetSwap() {
        _swapStep.value = SwapStep.IDLE
        _timerSeconds.value = 0
        loadStations()
    }

    fun topUpWallet(amount: Double) {
        viewModelScope.launch {
            _walletBalance.value += amount
            try {
                api.topUpWallet(
                    WalletTopUpRequest(
                        accountId = "99999999-9999-9999-9999-999999999999",
                        amount = amount,
                        paymentReference = "UPI-${System.currentTimeMillis()}"
                    )
                )
            } catch (_: Exception) {}
        }
    }
}
