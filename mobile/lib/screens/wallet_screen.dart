import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import '../models/models.dart';

class WalletScreen extends StatefulWidget {
  final DriverProfile profile;

  const WalletScreen({Key? key, required this.profile}) : super(key: key);

  @override
  State<WalletScreen> createState() => _WalletScreenState();
}

class _WalletScreenState extends State<WalletScreen> {
  final ApiClient _apiClient = ApiClient();
  double _balance = 450.0;
  bool _isProcessing = false;

  @override
  void initState() {
    super.initState();
    _balance = widget.profile.walletBalance;
  }

  void _topUp(double amount) async {
    setState(() => _isProcessing = true);
    final newBal = await _apiClient.topUpWallet(widget.profile.id, amount);
    setState(() {
      _balance = newBal;
      _isProcessing = false;
    });

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppColors.surfaceElevated,
          content: Text('₹${amount.toStringAsFixed(0)} added successfully via UPI AutoPay!'),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('ChargeLoop Wallet & Dues'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          // Balance Card
          Container(
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [Color(0xFF1E2836), Color(0xFF141C24)],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.border),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'TOTAL WALLET BALANCE',
                      style: TextStyle(color: AppColors.textSecondary, fontSize: 13, letterSpacing: 1.0),
                    ),
                    Icon(Icons.account_balance_wallet, color: AppColors.neonGreen, size: 24),
                  ],
                ),
                const SizedBox(height: 12),
                Text(
                  '₹${_balance.toStringAsFixed(2)}',
                  style: const TextStyle(
                    fontSize: 38,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Sufficient for approx. 3 more battery swaps',
                  style: TextStyle(color: AppColors.neonGreen, fontSize: 13),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // Quick Top-Up Section
          const Text(
            'Quick Recharge (UPI)',
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              _buildTopUpChip(100),
              const SizedBox(width: 10),
              _buildTopUpChip(250),
              const SizedBox(width: 10),
              _buildTopUpChip(500),
            ],
          ),
          const SizedBox(height: 30),

          // Recent Activity
          const Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Recent Transactions',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              Text(
                'GST Invoices',
                style: TextStyle(fontSize: 14, color: AppColors.brightCyan, fontWeight: FontWeight.w600),
              ),
            ],
          ),
          const SizedBox(height: 12),
          _buildTransactionTile('Battery Swap Fee (Janakpuri Hub)', '-₹118.00', 'Today, 2:15 PM', isDebit: true),
          _buildTransactionTile('UPI AutoPay Wallet Top-Up', '+₹500.00', 'Yesterday, 8:40 PM', isDebit: false),
          _buildTransactionTile('Welcome Driver Sign-Up Bonus', '+₹200.00', '12 Nov 2026', isDebit: false),
        ],
      ),
    );
  }

  Widget _buildTopUpChip(double amount) {
    return Expanded(
      child: OutlinedButton(
        style: OutlinedButton.styleFrom(
          padding: const EdgeInsets.symmetric(vertical: 14),
          side: const BorderSide(color: AppColors.neonGreen, width: 1.5),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          backgroundColor: AppColors.neonGreen.withOpacity(0.05),
        ),
        onPressed: _isProcessing ? null : () => _topUp(amount),
        child: Text(
          '+ ₹${amount.toInt()}',
          style: const TextStyle(
            color: AppColors.neonGreen,
            fontSize: 16,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
    );
  }

  Widget _buildTransactionTile(String title, String amount, String time, {required bool isDebit}) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: (isDebit ? AppColors.dangerRed : AppColors.neonGreen).withOpacity(0.15),
          child: Icon(
            isDebit ? Icons.arrow_outward : Icons.arrow_downward,
            color: isDebit ? AppColors.dangerRed : AppColors.neonGreen,
            size: 20,
          ),
        ),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14, color: Colors.white)),
        subtitle: Text(time, style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
        trailing: Text(
          amount,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            fontSize: 15,
            color: isDebit ? Colors.white : AppColors.neonGreen,
          ),
        ),
      ),
    );
  }
}
