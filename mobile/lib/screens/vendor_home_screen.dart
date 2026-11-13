import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../models/models.dart';
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
            Column(
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
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: _stationStatus == 0 ? AppColors.neonGreen.withOpacity(0.15) : AppColors.warningAmber.withOpacity(0.15),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: _stationStatus == 0 ? AppColors.neonGreen : AppColors.warningAmber),
              ),
              child: Text(
                _stationStatus == 0 ? '🟢 Station Open' : '🟡 Maintenance',
                style: TextStyle(
                  color: _stationStatus == 0 ? AppColors.neonGreen : AppColors.warningAmber,
                  fontWeight: FontWeight.bold,
                  fontSize: 11,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),

        // Metrics Overview
        Row(
          children: [
            Expanded(
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: const [
                      Text('8', style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold, color: AppColors.neonGreen)),
                      SizedBox(height: 4),
                      Text('Ready Batteries', style: TextStyle(fontSize: 12, color: AppColors.textSecondary)),
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
                    children: const [
                      Text('6', style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold, color: AppColors.warningAmber)),
                      SizedBox(height: 4),
                      Text('Charging Bays', style: TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),

        // Operational Status Switcher
        const Text('Operational Status Control', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: Colors.white)),
        const SizedBox(height: 10),
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
        const SizedBox(height: 24),
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
