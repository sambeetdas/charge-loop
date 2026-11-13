package com.chargeloop.driver.data.api

import com.chargeloop.driver.data.model.*
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.POST
import java.util.concurrent.TimeUnit

interface ChargeLoopApiService {
    @POST("api/v1/stations/nearby")
    suspend fun getNearbyStations(@Body request: NearbyStationsRequest): List<StationDto>

    @POST("api/v1/swaps/initiate")
    suspend fun initiateSwap(@Body request: InitiateSwapRequest): SwapResponse

    @POST("api/v1/swaps/confirm")
    suspend fun confirmSwap(@Body request: ConfirmSwapRequest): SwapResponse

    @POST("api/v1/wallet/topup")
    suspend fun topUpWallet(@Body request: WalletTopUpRequest): WalletBalanceResponse

    companion object {
        // 10.0.2.2 is the special alias to PC localhost from Android Emulator
        private const val BASE_URL = "http://10.0.2.2:5000/"

        fun create(): ChargeLoopApiService {
            val logging = HttpLoggingInterceptor().apply {
                level = HttpLoggingInterceptor.Level.BODY
            }
            val client = OkHttpClient.Builder()
                .addInterceptor(logging)
                .connectTimeout(5, TimeUnit.SECONDS)
                .readTimeout(5, TimeUnit.SECONDS)
                .build()

            return Retrofit.Builder()
                .baseUrl(BASE_URL)
                .client(client)
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(ChargeLoopApiService::class.java)
        }
    }
}
