import 'dart:async';
import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import '../models/models.dart';
import '../widgets/battery_gauge.dart';

class SwapScreen extends StatefulWidget {
  final DriverProfile profile;
  final Station? initialStation;

  const SwapScreen({Key? key, required this.profile, this.initialStation}) : super(key: key);

  @override
  State<SwapScreen> createState() => _SwapScreenState();
}

class _SwapScreenState extends State<SwapScreen> {
  final ApiClient _apiClient = ApiClient();
  int _swapStep = 0; // 0: Ready, 1: Validating, 2: Exchanging, 3: Completed
  bool _isLoading = false;
  SwapRecord? _currentSwap;

  List<Station> _stations = [];
  Station? _selectedStation;
  List<BatteryModel> _readyBatteries = [];
  BatteryModel? _selectedIssuedBattery;
  final TextEditingController _depletedSerialController = TextEditingController();
  int _depletedSoc = 18;

  Timer? _timer;
  int _secondsElapsed = 0;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  void _loadData() async {
    try {
      final list = await _apiClient.getNearbyStations();
      final driverDetails = await _apiClient.getDriverDetails(widget.profile.id);
      if (mounted) {
        setState(() {
          _stations = list;
          _selectedStation = widget.initialStation ?? (list.isNotEmpty ? list.first : null);
          if (driverDetails.activeBattery != null) {
            _depletedSerialController.text = driverDetails.activeBattery!.serialNumber;
            _depletedSoc = driverDetails.activeBattery!.stateOfChargePercent;
          }
        });
        if (_selectedStation != null) {
          _loadStationBatteries(_selectedStation!.id);
        }
      }
    } catch (_) {}
  }

  void _loadStationBatteries(String stationId) async {
    try {
      final batteries = await _apiClient.getStationBatteries(stationId);
      if (mounted) {
        setState(() {
          _readyBatteries = batteries;
          _selectedIssuedBattery = batteries.isNotEmpty ? batteries.first : null;
        });
      }
    } catch (_) {}
  }

  void _startSwap() async {
    if (_selectedStation == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Please select a station')));
      return;
    }

    if (_selectedIssuedBattery == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('No charged batteries ready at this station')));
      return;
    }

    final depletedSerial = _depletedSerialController.text.trim();
    if (depletedSerial.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Please enter or scan your depleted battery serial')));
      return;
    }

    final issuedSerial = _selectedIssuedBattery!.serialNumber;

    setState(() {
      _isLoading = true;
      _swapStep = 1;
    });

    try {
      final swapRes = await _apiClient.initiateSwap(
        driverId: widget.profile.id,
        stationId: _selectedStation!.id,
        partnerId: _selectedStation!.partnerId,
        depletedBatterySerial: depletedSerial,
        issuedBatterySerial: issuedSerial,
      );

      setState(() {
        _currentSwap = SwapRecord(
          swapId: swapRes.swapId,
          status: swapRes.status,
          depletedSerial: swapRes.depletedSerial,
          depletedSoc: swapRes.depletedSoc,
          issuedSerial: swapRes.issuedSerial,
          issuedSoc: swapRes.issuedSoc,
          totalAmount: swapRes.totalAmount,
          message: swapRes.message,
        );
        _isLoading = false;
        _swapStep = 2; // Physical exchange
        _secondsElapsed = 0;
      });

      _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
        setState(() => _secondsElapsed++);
      });
    } catch (e) {
      setState(() {
        _isLoading = false;
        _swapStep = 0;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(backgroundColor: AppColors.dangerRed, content: Text('Error: $e')));
      }
    }
  }

  void _completeSwap() async {
    _timer?.cancel();
    setState(() => _isLoading = true);

    if (_currentSwap != null) {
      try {
        final confirmed = await _apiClient.confirmSwap(
          swapId: _currentSwap!.swapId,
          paymentMethod: 1, // Wallet
        );

        setState(() {
          _currentSwap = confirmed;
          _isLoading = false;
          _swapStep = 3; // Done
        });
      } catch (e) {
        setState(() => _isLoading = false);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(backgroundColor: AppColors.dangerRed, content: Text('Error: $e')));
        }
      }
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
    _depletedSerialController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Battery Swap'),
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
      child: ListView(
        children: [
          Center(child: BatteryGauge(percentage: _depletedSoc, size: 130)),
          const SizedBox(height: 16),
          Center(
            child: Text(
              'Battery Level: $_depletedSoc%',
              style: TextStyle(
                fontSize: 18, 
                fontWeight: FontWeight.bold, 
                color: _depletedSoc < 20 ? AppColors.dangerRed : (_depletedSoc < 50 ? AppColors.warningAmber : AppColors.neonGreen)
              ),
            ),
          ),
          const SizedBox(height: 24),
          const Text('Select Swapping Station', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Colors.white)),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: AppColors.border),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<Station>(
                isExpanded: true,
                dropdownColor: AppColors.surface,
                value: _selectedStation,
                items: _stations.map((s) {
                  return DropdownMenuItem(
                    value: s,
                    child: Text('${s.name} (${s.availableCharged} Ready)', style: const TextStyle(color: Colors.white, fontSize: 14)),
                  );
                }).toList(),
                onChanged: (val) {
                  if (val != null) {
                    setState(() => _selectedStation = val);
                    _loadStationBatteries(val.id);
                  }
                },
              ),
            ),
          ),
          const SizedBox(height: 16),
          const Text('Returned Battery Barcode / Serial', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Colors.white)),
          const SizedBox(height: 8),
          Container(
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: AppColors.border),
            ),
            child: TextField(
              controller: _depletedSerialController,
              style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.qr_code_scanner, color: AppColors.warningAmber),
                border: InputBorder.none,
                contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 14),
                hintText: 'Enter or scan battery serial',
                hintStyle: TextStyle(color: AppColors.textSecondary),
              ),
            ),
          ),
          const SizedBox(height: 16),
          const Text('Ready Charged Battery at Station', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Colors.white)),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: AppColors.border),
            ),
            child: _readyBatteries.isEmpty
                ? const Padding(
                    padding: EdgeInsets.symmetric(vertical: 14),
                    child: Text('No charged batteries currently available', style: TextStyle(color: AppColors.textSecondary, fontSize: 14)),
                  )
                : DropdownButtonHideUnderline(
                    child: DropdownButton<BatteryModel>(
                      isExpanded: true,
                      dropdownColor: AppColors.surface,
                      value: _selectedIssuedBattery,
                      items: _readyBatteries.map((b) {
                        return DropdownMenuItem(
                          value: b,
                          child: Text('${b.serialNumber} (${b.stateOfChargePercent}% Full, ${b.modelType})', style: const TextStyle(color: Colors.white, fontSize: 14)),
                        );
                      }).toList(),
                      onChanged: (val) {
                        if (val != null) setState(() => _selectedIssuedBattery = val);
                      },
                    ),
                  ),
          ),
          const SizedBox(height: 28),
          ElevatedButton.icon(
            onPressed: _startSwap,
            icon: const Icon(Icons.flash_on),
            label: const Text('Initiate Battery Swap', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          ),
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
            Text('Validating Station Handshake (< 1.5s)...', style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: Colors.white)),
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
                const Text('SWAP STOPWATCH (TARGET: < 2:00)', style: TextStyle(color: AppColors.textSecondary, fontSize: 13, letterSpacing: 1.0)),
                const SizedBox(height: 10),
                Text('$minutes:$seconds', style: const TextStyle(fontSize: 48, fontWeight: FontWeight.w900, color: AppColors.brightCyan, fontFamily: 'monospace')),
              ],
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                children: [
                  _buildBatteryRow('Returned Battery', _currentSwap?.depletedSerial ?? '', '${_currentSwap?.depletedSoc ?? 16}%', AppColors.warningAmber),
                  const Divider(color: AppColors.border, height: 24),
                  _buildBatteryRow('Issued Battery', _currentSwap?.issuedSerial ?? '', '100% Full', AppColors.neonGreen),
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
          const Text('Swap Completed Successfully!', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white)),
          const SizedBox(height: 6),
          Text('Duration: ${_secondsElapsed > 0 ? "$_secondsElapsed seconds" : "Completed"}', style: const TextStyle(color: AppColors.neonGreen, fontSize: 15, fontWeight: FontWeight.w600)),
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
                  _buildReceiptRow('Partner Commission', '₹25.00 Credited'),
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
          decoration: BoxDecoration(color: socColor.withOpacity(0.15), borderRadius: BorderRadius.circular(6)),
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
