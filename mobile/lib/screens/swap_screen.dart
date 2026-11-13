import 'dart:async';
import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import '../models/models.dart';
import '../widgets/battery_gauge.dart';

class SwapScreen extends StatefulWidget {
  final DriverProfile profile;

  const SwapScreen({Key? key, required this.profile}) : super(key: key);

  @override
  State<SwapScreen> createState() => _SwapScreenState();
}

class _SwapScreenState extends State<SwapScreen> {
  final ApiClient _apiClient = ApiClient();
  int _swapStep = 0; // 0: Ready to scan, 1: Validating, 2: Exchanging, 3: Completed
  bool _isLoading = false;
  SwapRecord? _currentSwap;

  Timer? _timer;
  int _secondsElapsed = 0;

  void _startSwap() async {
    setState(() {
      _isLoading = true;
      _swapStep = 1;
    });

    // Simulate rapid QR scan of station and batteries
    final swap = await _apiClient.initiateSwap(
      driverId: widget.profile.id,
      stationId: 'aaaa1111-aaaa-1111-aaaa-111111111111',
      partnerId: '11111111-1111-1111-1111-111111111111',
      depletedBatterySerial: 'BAT-48V-9999',
      issuedBatterySerial: 'BAT-48V-1001',
    );

    setState(() {
      _currentSwap = swap;
      _isLoading = false;
      _swapStep = 2; // In physical exchange
      _secondsElapsed = 0;
    });

    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      setState(() => _secondsElapsed++);
    });
  }

  void _completeSwap() async {
    _timer?.cancel();
    setState(() => _isLoading = true);

    if (_currentSwap != null) {
      final confirmed = await _apiClient.confirmSwap(
        swapId: _currentSwap!.swapId,
        paymentMethod: 1, // Wallet
      );

      setState(() {
        _currentSwap = confirmed;
        _isLoading = false;
        _swapStep = 3; // Done
      });
    }
  }

  void _reset() {
    _timer?.cancel();
    setState(() {
      _swapStep = 0;
      _currentSwap = null;
      _secondsElapsed = 0;
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('2-Minute Battery Swap'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          children: [
            if (_swapStep == 0) _buildInitialStep(),
            if (_swapStep == 1) _buildValidatingStep(),
            if (_swapStep == 2) _buildExchangingStep(),
            if (_swapStep == 3) _buildCompletedStep(),
          ],
        ),
      ),
    );
  }

  // Step 0: Ready
  Widget _buildInitialStep() {
    return Expanded(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const BatteryGauge(percentage: 16, size: 140),
          const SizedBox(height: 24),
          const Text(
            'Low Battery Warning (16%)',
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: AppColors.dangerRed,
            ),
          ),
          const SizedBox(height: 8),
          const Text(
            'Estimated Range: ~12 km remaining',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 14),
          ),
          const Spacer(),
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.border),
            ),
            child: Row(
              children: [
                const Icon(Icons.qr_code_scanner, color: AppColors.neonGreen, size: 36),
                const SizedBox(width: 14),
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Janakpuri Swap Hub',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Colors.white),
                      ),
                      Text('Operator: Ramesh Kumar (Kirana)', style: TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppColors.neonGreen.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: const Text('8 Ready', style: TextStyle(color: AppColors.neonGreen, fontWeight: FontWeight.bold)),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          ElevatedButton.icon(
            onPressed: _startSwap,
            icon: const Icon(Icons.qr_code),
            label: const Text('Scan & Initiate 2-Min Swap', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          ),
          const SizedBox(height: 16),
        ],
      ),
    );
  }

  // Step 1: Validating
  Widget _buildValidatingStep() {
    return const Expanded(
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            CircularProgressIndicator(color: AppColors.neonGreen, strokeWidth: 3),
            SizedBox(height: 20),
            Text(
              'Handshake in progress (< 1.5s)...',
              style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: Colors.white),
            ),
            SizedBox(height: 8),
            Text(
              'Verifying battery SOH, health & subscription rules',
              style: TextStyle(color: AppColors.textSecondary),
            ),
          ],
        ),
      ),
    );
  }

  // Step 2: Physical Swap in progress
  Widget _buildExchangingStep() {
    final minutes = (_secondsElapsed ~/ 60).toString().padLeft(2, '0');
    final seconds = (_secondsElapsed % 60).toString().padLeft(2, '0');

    return Expanded(
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.brightCyan.withOpacity(0.4)),
            ),
            child: Column(
              children: [
                const Text(
                  'SWAP TIMER (TARGET: < 2:00)',
                  style: TextStyle(color: AppColors.textSecondary, fontSize: 13, letterSpacing: 1.0),
                ),
                const SizedBox(height: 10),
                Text(
                  '$minutes:$seconds',
                  style: const TextStyle(
                    fontSize: 48,
                    fontWeight: FontWeight.w900,
                    color: AppColors.brightCyan,
                    fontFamily: 'monospace',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                children: [
                  _buildBatteryRow('Returned Battery', _currentSwap?.depletedSerial ?? 'BAT-OLD', '${_currentSwap?.depletedSoc ?? 16}%', AppColors.warningAmber),
                  const Divider(color: AppColors.border, height: 24),
                  _buildBatteryRow('New Charged Battery', _currentSwap?.issuedSerial ?? 'BAT-NEW', '100% Full', AppColors.neonGreen),
                ],
              ),
            ),
          ),
          const Spacer(),
          ElevatedButton(
            onPressed: _completeSwap,
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.neonGreen),
            child: const Text('Confirm Physical Swap Completed', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  // Step 3: Completed Receipt
  Widget _buildCompletedStep() {
    return Expanded(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.check_circle, color: AppColors.neonGreen, size: 72),
          const SizedBox(height: 16),
          const Text(
            'Swap Completed Successfully!',
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
          ),
          const SizedBox(height: 6),
          Text(
            'Duration: ${_secondsElapsed > 0 ? "$_secondsElapsed seconds" : "1 min 42 sec"}',
            style: const TextStyle(color: AppColors.neonGreen, fontSize: 15, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                children: [
                  _buildReceiptRow('Base Swap Fee', '₹100.00'),
                  _buildReceiptRow('GST (18%)', '₹18.00'),
                  const Divider(color: AppColors.border),
                  _buildReceiptRow('Total Charged', '₹118.00', isBold: true),
                  _buildReceiptRow('Payment Method', 'Driver Wallet'),
                  _buildReceiptRow('Partner Instant Share', '₹25.00 Credited'),
                ],
              ),
            ),
          ),
          const Spacer(),
          ElevatedButton(
            onPressed: _reset,
            child: const Text('Back to Home & Resume Driving', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  Widget _buildBatteryRow(String label, String serial, String soc, Color socColor) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
            const SizedBox(height: 2),
            Text(serial, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Colors.white)),
          ],
        ),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
          decoration: BoxDecoration(
            color: socColor.withOpacity(0.15),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Text(soc, style: TextStyle(color: socColor, fontWeight: FontWeight.bold, fontSize: 13)),
        ),
      ],
    );
  }

  Widget _buildReceiptRow(String label, String val, {bool isBold = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: isBold ? Colors.white : AppColors.textSecondary, fontWeight: isBold ? FontWeight.bold : FontWeight.normal)),
          Text(val, style: TextStyle(color: isBold ? AppColors.neonGreen : Colors.white, fontWeight: isBold ? FontWeight.bold : FontWeight.normal)),
        ],
      ),
    );
  }
}
