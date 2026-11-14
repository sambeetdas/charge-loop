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
  final TextEditingController _refController = TextEditingController();
  double _balance = 450.0;
  bool _isProcessing = false;
  String _paymentMode = 'CARD';

  @override
  void initState() {
    super.initState();
    _balance = widget.profile.walletBalance;
  }

  void _topUp(double amount) async {
    setState(() => _isProcessing = true);
    final newBal = await _apiClient.topUpWallet(widget.profile.id, amount, paymentMethod: _paymentMode);
    setState(() {
      _balance = newBal;
      _isProcessing = false;
    });

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppColors.surfaceElevated,
          content: Text('₹${amount.toStringAsFixed(0)} added successfully via $_paymentMode!'),
        ),
      );
    }
  }

  void _claimReferral() async {
    final code = _refController.text.trim();
    if (code.isEmpty) return;
    setState(() => _isProcessing = true);
    try {
      final res = await _apiClient.claimReferral(widget.profile.id, code);
      setState(() {
        _balance = (res['newWalletBalance'] as num?)?.toDouble() ?? _balance;
        _isProcessing = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(res['message'] ?? 'Referral bonus credited!')),
        );
      }
    } catch (e) {
      setState(() => _isProcessing = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(backgroundColor: AppColors.dangerRed, content: Text('$e')),
        );
      }
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
                colors: [AppColors.surfaceElevated, AppColors.surface],
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
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Quick Recharge',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              ToggleButtons(
                isSelected: [_paymentMode == 'CARD', _paymentMode == 'UPI'],
                onPressed: (idx) => setState(() => _paymentMode = idx == 0 ? 'CARD' : 'UPI'),
                borderRadius: BorderRadius.circular(10),
                selectedColor: Colors.black,
                fillColor: AppColors.neonGreen,
                color: AppColors.textSecondary,
                constraints: const BoxConstraints(minHeight: 32, minWidth: 64),
                children: const [Text('Card'), Text('UPI')],
              ),
            ],
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
          const SizedBox(height: 20),

          // Refer Your Friend Card
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Refer Your Friend', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      Text(widget.profile.referralCode ?? 'CL-FRIEND', style: const TextStyle(color: AppColors.neonGreen, fontWeight: FontWeight.bold)),
                    ],
                  ),
                  const SizedBox(height: 6),
                  const Text('Earn ₹50 each when a friend joins with your code.', style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _refController,
                          decoration: const InputDecoration(hintText: 'Enter friend code', isDense: true),
                        ),
                      ),
                      const SizedBox(width: 8),
                      ElevatedButton(
                        style: ElevatedButton.styleFrom(minimumSize: const Size(84, 42), backgroundColor: AppColors.warningAmber),
                        onPressed: _isProcessing ? null : _claimReferral,
                        child: const Text('Claim'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),

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
