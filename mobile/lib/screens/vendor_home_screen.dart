import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import '../models/models.dart';
import '../widgets/qr_scanner_sheet.dart';
import 'auth_screen.dart';

class VendorHomeScreen extends StatefulWidget {
  final VendorProfile vendor;

  const VendorHomeScreen({Key? key, required this.vendor}) : super(key: key);

  @override
  State<VendorHomeScreen> createState() => _VendorHomeScreenState();
}

class _VendorHomeScreenState extends State<VendorHomeScreen> {
  int _currentIndex = 0;
  int _stationStatus = 0; // 0=Open, 1=Closed, 2=Maintenance
  List<StationModel> _stations = [];
  StationModel? _selectedStation;
  bool _isLoadingStations = true;

  // Partner Swap Desk State
  final _driverQueryCtrl = TextEditingController();
  final _depletedCtrl = TextEditingController(text: 'BAT-LFP-1001');
  final _issuedCtrl = TextEditingController(text: 'BAT-LFP-NEW01');
  final _cashCtrl = TextEditingController(text: '118');
  Map<String, dynamic>? _validatedDriver;
  bool _deskBusy = false;

  @override
  void initState() {
    super.initState();
    _loadVendorStations();
  }

  void _loadVendorStations() async {
    try {
      final list = await ApiClient().getVendorStations(widget.vendor.id);
      if (mounted) {
        setState(() {
          _stations = list;
          if (_stations.isNotEmpty) {
            _selectedStation = _stations.first;
            _stationStatus = _selectedStation!.status;
          }
          _isLoadingStations = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoadingStations = false);
    }
  }

  void _showRegisterBatteryDialog() {
    if (_selectedStation == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please add/select a station first.')),
      );
      return;
    }
    final serialCtrl = TextEditingController();
    final modelCtrl = TextEditingController(text: 'LFP-48V-30Ah');
    final socCtrl = TextEditingController(text: '100');

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.surface,
        title: Text('Register Battery — ${_selectedStation!.name}', style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(controller: serialCtrl, style: const TextStyle(color: Colors.white), decoration: const InputDecoration(labelText: 'Battery Serial / QR')),
            const SizedBox(height: 10),
            TextField(controller: modelCtrl, style: const TextStyle(color: Colors.white), decoration: const InputDecoration(labelText: 'Model Type')),
            const SizedBox(height: 10),
            TextField(controller: socCtrl, keyboardType: TextInputType.number, style: const TextStyle(color: Colors.white), decoration: const InputDecoration(labelText: 'SOC % (0-100)')),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () async {
              if (serialCtrl.text.trim().isEmpty) return;
              try {
                await ApiClient().registerBatteryAtStation(
                  stationId: _selectedStation!.id,
                  serialNumber: serialCtrl.text.trim(),
                  modelType: modelCtrl.text.trim(),
                  soc: int.tryParse(socCtrl.text.trim()) ?? 100,
                );
                if (ctx.mounted) Navigator.pop(ctx);
                _loadVendorStations();
                if (mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Battery registered at store!')));
                }
              } catch (e) {
                if (mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(SnackBar(backgroundColor: AppColors.dangerRed, content: Text('$e')));
                }
              }
            },
            child: const Text('Register'),
          ),
        ],
      ),
    );
  }

  void _validateDriver() async {
    if (_driverQueryCtrl.text.trim().isEmpty) return;
    setState(() => _deskBusy = true);
    try {
      final res = await ApiClient().validateDriverForSwap(_driverQueryCtrl.text.trim());
      setState(() {
        _validatedDriver = res;
        if (res['activeBatterySerial'] != null) {
          _depletedCtrl.text = res['activeBatterySerial'];
        }
        _deskBusy = false;
      });
    } catch (e) {
      setState(() {
        _validatedDriver = null;
        _deskBusy = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(backgroundColor: AppColors.dangerRed, content: Text('$e')));
      }
    }
  }

  void _executePartnerCashSwap() async {
    if (_validatedDriver == null || _selectedStation == null) return;
    setState(() => _deskBusy = true);
    try {
      final init = await ApiClient().initiateSwap(
        driverId: _validatedDriver!['driverId'],
        stationId: _selectedStation!.id,
        partnerId: widget.vendor.id,
        depletedBatterySerial: _depletedCtrl.text.trim(),
        issuedBatterySerial: _issuedCtrl.text.trim(),
      );
      await ApiClient().confirmSwap(
        swapId: init.swapId,
        paymentMethod: 3, // Cash
        cashAmount: double.tryParse(_cashCtrl.text.trim()) ?? 118.0,
      );
      setState(() => _deskBusy = false);
      _loadVendorStations();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Cash swap completed & logged!')));
      }
    } catch (e) {
      setState(() => _deskBusy = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(backgroundColor: AppColors.dangerRed, content: Text('$e')));
      }
    }
  }

  void _showAddStationDialog() {
    final nameCtrl = TextEditingController();
    final addrCtrl = TextEditingController();
    final cityCtrl = TextEditingController(text: 'New Delhi');
    final baysCtrl = TextEditingController(text: '12');

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.surface,
        title: const Text('Register New Station / Hub', style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: nameCtrl,
                style: const TextStyle(color: Colors.white),
                decoration: const InputDecoration(labelText: 'Station / Hub Name', hintText: 'e.g. Metro Swapping Hub'),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: addrCtrl,
                style: const TextStyle(color: Colors.white),
                decoration: const InputDecoration(labelText: 'Address', hintText: 'e.g. Sector 14 Metro Station'),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: cityCtrl,
                style: const TextStyle(color: Colors.white),
                decoration: const InputDecoration(labelText: 'City'),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: baysCtrl,
                keyboardType: TextInputType.number,
                style: const TextStyle(color: Colors.white),
                decoration: const InputDecoration(labelText: 'Total Charging Bays'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Cancel', style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton(
            onPressed: () async {
              final name = nameCtrl.text.trim();
              final addr = addrCtrl.text.trim();
              final city = cityCtrl.text.trim();
              final bays = int.tryParse(baysCtrl.text.trim()) ?? 12;

              if (name.isEmpty || addr.isEmpty) return;

              Navigator.pop(ctx);
              try {
                final created = await ApiClient().createStation(
                  partnerId: widget.vendor.id,
                  name: name,
                  address: addr,
                  city: city,
                  totalBays: bays,
                );
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(backgroundColor: AppColors.neonGreen, content: Text('Station "${created.name}" registered!')),
                );
                _loadVendorStations();
              } catch (e) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(backgroundColor: AppColors.dangerRed, content: Text('Error: $e')),
                );
              }
            },
            child: const Text('Register Station'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final screens = [
      _buildOperationsScreen(),
      _buildEarningsScreen(),
      _buildSupportScreen(),
      _buildProfileScreen(),
    ];

    return Scaffold(
      body: SafeArea(
        child: screens[_currentIndex],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (index) => setState(() => _currentIndex = index),
        type: BottomNavigationBarType.fixed,
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.ev_station),
            label: 'Operations',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.account_balance_wallet_outlined),
            label: 'Earnings',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.build_outlined),
            label: 'Support',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person_outline),
            label: 'Profile',
          ),
        ],
      ),
    );
  }

  // 1. Station Operations Screen (NO live bay grid, only status and metrics)
  Widget _buildOperationsScreen() {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.vendor.businessName,
                    style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Operator: ${widget.vendor.fullName}',
                    style: const TextStyle(color: AppColors.textSecondary, fontSize: 13),
                  ),
                ],
              ),
            ),
            ElevatedButton.icon(
              onPressed: _showAddStationDialog,
              icon: const Icon(Icons.add, size: 16),
              label: const Text('Add Hub', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.neonGreen,
                foregroundColor: Colors.black,
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),

        // Station Selector Dropdown (Multi-station support)
        if (_stations.isNotEmpty) ...[
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: AppColors.border),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<StationModel>(
                value: _selectedStation,
                isExpanded: true,
                dropdownColor: AppColors.surfaceElevated,
                items: _stations.map((s) {
                  return DropdownMenuItem<StationModel>(
                    value: s,
                    child: Text('${s.name} (${s.city})', style: const TextStyle(color: Colors.white, fontSize: 14)),
                  );
                }).toList(),
                onChanged: (val) {
                  if (val != null) {
                    setState(() {
                      _selectedStation = val;
                      _stationStatus = val.status;
                    });
                  }
                },
              ),
            ),
          ),
          const SizedBox(height: 16),
        ] else if (!_isLoadingStations) ...[
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  const Icon(Icons.ev_station, size: 40, color: AppColors.neonGreen),
                  const SizedBox(height: 10),
                  const Text('No Stations Registered Yet', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white)),
                  const SizedBox(height: 6),
                  const Text('Tap "Add Hub" to register your first swapping station.', style: TextStyle(color: AppColors.textSecondary, fontSize: 13), textAlign: TextAlign.center),
                  const SizedBox(height: 14),
                  ElevatedButton(
                    onPressed: _showAddStationDialog,
                    child: const Text('Register First Station'),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
        ],

        // Metrics Overview
        Row(
          children: [
            Expanded(
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(
                        '${_selectedStation?.availableCharged ?? 0}',
                        style: const TextStyle(fontSize: 32, fontWeight: FontWeight.bold, color: AppColors.neonGreen),
                      ),
                      const SizedBox(height: 4),
                      const Text('Ready Batteries', style: TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                    ],
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(
                        '${_selectedStation?.charging ?? 0}',
                        style: const TextStyle(fontSize: 32, fontWeight: FontWeight.bold, color: AppColors.warningAmber),
                      ),
                      const SizedBox(height: 4),
                      const Text('Charging Bays', style: TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),

        // Operational Status Switcher
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text('Operational Status Control', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Colors.white)),
            TextButton.icon(
              onPressed: _showRegisterBatteryDialog,
              icon: const Icon(Icons.battery_charging_full, size: 16, color: AppColors.neonGreen),
              label: const Text('+ Register Battery', style: TextStyle(color: AppColors.neonGreen)),
            ),
          ],
        ),
        const SizedBox(height: 10),

        // Partner Swap Desk Card (Validate -> QR -> Cash Amount -> Swap)
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Partner Swap Desk (Cash)', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Colors.white)),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _driverQueryCtrl,
                        style: const TextStyle(color: Colors.white),
                        decoration: const InputDecoration(hintText: 'Driver Phone / Name', isDense: true),
                      ),
                    ),
                    const SizedBox(width: 8),
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(minimumSize: const Size(90, 42)),
                      onPressed: _deskBusy ? null : _validateDriver,
                      child: const Text('Validate'),
                    ),
                  ],
                ),
                if (_validatedDriver != null) ...[
                  const SizedBox(height: 8),
                  Text(
                    '✓ ${_validatedDriver!['fullName']} (${_validatedDriver!['phoneNumber']})',
                    style: const TextStyle(color: AppColors.neonGreen, fontWeight: FontWeight.bold, fontSize: 13),
                  ),
                  const SizedBox(height: 8),
                  TextField(
                    controller: _depletedCtrl,
                    style: const TextStyle(color: Colors.white),
                    decoration: InputDecoration(
                      labelText: 'Depleted Battery QR (Disconnect)',
                      isDense: true,
                      suffixIcon: IconButton(
                        icon: const Icon(Icons.qr_code_scanner, color: AppColors.neonGreen),
                        onPressed: () async {
                          final scanned = await QrScannerSheet.scan(context, title: 'Scan Depleted Battery QR');
                          if (scanned != null && mounted) setState(() => _depletedCtrl.text = scanned);
                        },
                      ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextField(
                    controller: _issuedCtrl,
                    style: const TextStyle(color: Colors.white),
                    decoration: InputDecoration(
                      labelText: 'New Battery QR (Assign)',
                      isDense: true,
                      suffixIcon: IconButton(
                        icon: const Icon(Icons.qr_code_scanner, color: AppColors.neonGreen),
                        onPressed: () async {
                          final scanned = await QrScannerSheet.scan(context, title: 'Scan New Charged Battery QR');
                          if (scanned != null && mounted) setState(() => _issuedCtrl.text = scanned);
                        },
                      ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextField(
                    controller: _cashCtrl,
                    keyboardType: TextInputType.number,
                    style: const TextStyle(color: Colors.white),
                    decoration: const InputDecoration(labelText: 'Cash Collected (₹)', isDense: true),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton(
                          onPressed: _deskBusy ? null : _executePartnerCashSwap,
                          child: const Text('Execute Cash Swap'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      OutlinedButton(
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              backgroundColor: AppColors.warningAmber,
                              content: Text('Work in progress to integrate online payment. Please wait for few days.', style: TextStyle(color: Colors.black, fontWeight: FontWeight.bold)),
                            ),
                          );
                        },
                        child: const Text('Card/UPI'),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: () => setState(() => _stationStatus = 0),
                style: OutlinedButton.styleFrom(
                  side: BorderSide(color: _stationStatus == 0 ? AppColors.neonGreen : AppColors.border),
                  backgroundColor: _stationStatus == 0 ? AppColors.neonGreen.withOpacity(0.1) : Colors.transparent,
                ),
                child: const Text('Open'),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: OutlinedButton(
                onPressed: () => setState(() => _stationStatus = 2),
                style: OutlinedButton.styleFrom(
                  side: BorderSide(color: _stationStatus == 2 ? AppColors.warningAmber : AppColors.border),
                  backgroundColor: _stationStatus == 2 ? AppColors.warningAmber.withOpacity(0.1) : Colors.transparent,
                ),
                child: const Text('Maintenance'),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: OutlinedButton(
                onPressed: () => setState(() => _stationStatus = 1),
                style: OutlinedButton.styleFrom(
                  side: BorderSide(color: _stationStatus == 1 ? AppColors.dangerRed : AppColors.border),
                  backgroundColor: _stationStatus == 1 ? AppColors.dangerRed.withOpacity(0.1) : Colors.transparent,
                ),
                child: const Text('Closed'),
              ),
            ),
          ],
        ),
      ],
    );
  }

  // 2. Earnings Screen
  Widget _buildEarningsScreen() {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('VENDOR COMMISSION BALANCE', style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                const SizedBox(height: 6),
                Text('₹${widget.vendor.walletBalance.toStringAsFixed(2)}', style: const TextStyle(fontSize: 34, fontWeight: FontWeight.bold, color: AppColors.brightCyan)),
                const SizedBox(height: 4),
                const Text('₹25.00 Instant Payout per Completed Swap', style: TextStyle(color: AppColors.neonGreen, fontSize: 12, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        ElevatedButton(
          onPressed: () {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Instant payout initiated to your registered UPI ID!')),
            );
          },
          child: const Text('Withdraw Commission to UPI / Bank'),
        ),
      ],
    );
  }

  // 3. Support Screen
  Widget _buildSupportScreen() {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const Text('Report Charger / Station Hardware Defect', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white)),
        const SizedBox(height: 8),
        const Text('Notify operations team immediately of connector wear, power fluctuations, or hardware issues.', style: TextStyle(color: AppColors.textSecondary, fontSize: 13)),
        const SizedBox(height: 20),
        TextField(
          decoration: const InputDecoration(
            labelText: 'Defect Subject (e.g. Bay 3 Lock Pin Jammed)',
            filled: true,
            fillColor: AppColors.surface,
          ),
        ),
        const SizedBox(height: 14),
        TextField(
          maxLines: 3,
          decoration: const InputDecoration(
            labelText: 'Detailed Description',
            filled: true,
            fillColor: AppColors.surface,
          ),
        ),
        const SizedBox(height: 20),
        ElevatedButton(
          onPressed: () {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Defect ticket logged with operations!')),
            );
          },
          child: const Text('Submit Hardware Ticket'),
        ),
      ],
    );
  }

  // 4. Vendor Profile Screen
  Widget _buildProfileScreen() {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Center(
          child: Column(
            children: [
              const CircleAvatar(
                radius: 36,
                backgroundColor: AppColors.surfaceElevated,
                child: Icon(Icons.storefront, size: 40, color: AppColors.brightCyan),
              ),
              const SizedBox(height: 12),
              Text(
                widget.vendor.businessName,
                style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              const SizedBox(height: 4),
              Text(
                'Owner: ${widget.vendor.fullName}',
                style: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
              ),
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.brightCyan.withOpacity(0.15),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: AppColors.brightCyan.withOpacity(0.4)),
                ),
                child: const Text(
                  'Authorized Station Vendor',
                  style: TextStyle(color: AppColors.brightCyan, fontWeight: FontWeight.bold, fontSize: 12),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 28),
        const Text('Vendor Contact Details', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Colors.white)),
        const SizedBox(height: 8),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              children: [
                _buildInfoRow('Mobile Number', widget.vendor.phoneNumber),
                const Divider(color: AppColors.border),
                _buildInfoRow('Payout Route', widget.vendor.upiId ?? 'partner@okhdfcbank'),
                const Divider(color: AppColors.border),
                _buildInfoRow('Commission Rate', '₹25.00 / Swap'),
              ],
            ),
          ),
        ),
        const SizedBox(height: 20),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text('Operated Hubs (${_stations.length})', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Colors.white)),
            TextButton.icon(
              onPressed: _showAddStationDialog,
              icon: const Icon(Icons.add, size: 16, color: AppColors.brightCyan),
              label: const Text('Add Hub', style: TextStyle(color: AppColors.brightCyan, fontSize: 13, fontWeight: FontWeight.bold)),
            ),
          ],
        ),
        const SizedBox(height: 6),
        if (_stations.isEmpty)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: const Text('No stations registered yet under this vendor.', style: TextStyle(color: AppColors.textSecondary, fontSize: 13)),
            ),
          )
        else
          ..._stations.map((s) => Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(s.name, style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.white, fontSize: 14)),
                          const SizedBox(height: 2),
                          Text('${s.address} • ${s.city}', style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                        ],
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.neonGreen.withOpacity(0.12),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text('⚡ ${s.availableCharged} Ready', style: const TextStyle(color: AppColors.neonGreen, fontSize: 11, fontWeight: FontWeight.bold)),
                      ),
                    ],
                  ),
                ),
              )),
        const SizedBox(height: 20),
        OutlinedButton(
          onPressed: () {
            Navigator.pushReplacement(
              context,
              MaterialPageRoute(builder: (_) => const AuthScreen()),
            );
          },
          style: OutlinedButton.styleFrom(foregroundColor: AppColors.textSecondary),
          child: const Text('Sign Out / Switch Account'),
        ),
        const SizedBox(height: 24),

        // Brand Footer
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(
            children: [
              RichText(
                textAlign: TextAlign.center,
                text: const TextSpan(
                  text: 'Swap. Ride. ',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w900,
                    color: Colors.white,
                  ),
                  children: [
                    TextSpan(
                      text: 'Repeat.',
                      style: TextStyle(color: AppColors.neonGreen),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'Station Vendor Partner Portal • Quick Swaps',
                style: TextStyle(fontSize: 11, color: AppColors.textSecondary),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              const Text(
                'www.chargeloopsln.in',
                style: TextStyle(fontSize: 12, color: AppColors.brightCyan, fontWeight: FontWeight.bold),
              ),
            ],
          ),
        ),
        const SizedBox(height: 20),
      ],
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.white, fontSize: 13)),
        ],
      ),
    );
  }
}
