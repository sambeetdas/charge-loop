import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../models/models.dart';
import 'auth_screen.dart';

class ProfileScreen extends StatelessWidget {
  final DriverProfile profile;

  const ProfileScreen({Key? key, required this.profile}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Driver Profile & Welfare'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          // Driver Avatar & Name
          Center(
            child: Column(
              children: [
                const CircleAvatar(
                  radius: 40,
                  backgroundColor: AppColors.surfaceElevated,
                  child: Icon(Icons.person, size: 48, color: AppColors.neonGreen),
                ),
                const SizedBox(height: 12),
                Text(
                  profile.fullName,
                  style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
                ),
                const SizedBox(height: 4),
                Text(
                  profile.phoneNumber,
                  style: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
                ),
                const SizedBox(height: 10),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
                  decoration: BoxDecoration(
                    color: AppColors.neonGreen.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: AppColors.neonGreen.withOpacity(0.4)),
                  ),
                  child: const Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.verified, color: AppColors.neonGreen, size: 16),
                      SizedBox(width: 6),
                      Text(
                        'KYC & Parivahan Verified',
                        style: TextStyle(color: AppColors.neonGreen, fontWeight: FontWeight.bold, fontSize: 12),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 28),

          // Linked Vehicle Card
          const Text('Linked EV Vehicle', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Colors.white)),
          const SizedBox(height: 10),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Row(
                children: [
                  const Icon(Icons.two_wheeler, color: AppColors.brightCyan, size: 36),
                  const SizedBox(width: 14),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('DL 01 AB 1234', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Colors.white)),
                        SizedBox(height: 2),
                        Text('Quantum Energy - Plaza EV 2W', style: TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                        SizedBox(height: 4),
                        Text('Compatible: 2W_48V Battery', style: TextStyle(color: AppColors.brightCyan, fontSize: 12)),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.chevron_right, color: AppColors.textSecondary),
                    onPressed: () {},
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),

          // Welfare & Safety Section
          const Text('Driver Welfare & Safety', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Colors.white)),
          const SizedBox(height: 10),
          Card(
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.shield_outlined, color: AppColors.neonGreen),
                  title: const Text('Driver Welfare Fund Insurance', style: TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w600)),
                  subtitle: const Text('Active cover: ₹5,00,000 accidental cover', style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                  trailing: const Icon(Icons.check_circle, color: AppColors.neonGreen, size: 18),
                ),
                const Divider(color: AppColors.border, height: 1),
                ListTile(
                  leading: const Icon(Icons.emergency, color: AppColors.dangerRed),
                  title: const Text('SOS Emergency Helpline', style: TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w600)),
                  subtitle: const Text('24/7 Roadside & Battery Safety Alert', style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                  trailing: ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.dangerRed,
                      foregroundColor: Colors.white,
                      minimumSize: const Size(64, 34),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    onPressed: () {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('SOS Alert dispatched to ChargeLoop Operations team!')),
                      );
                    },
                    child: const Text('SOS', style: TextStyle(fontWeight: FontWeight.bold)),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 30),

          // Sign out button
          OutlinedButton(
            style: OutlinedButton.styleFrom(
              side: const BorderSide(color: AppColors.border),
              foregroundColor: AppColors.textSecondary,
              minimumSize: const Size.fromHeight(48),
            ),
            onPressed: () {
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(builder: (_) => const AuthScreen()),
              );
            },
            child: const Text('Sign Out'),
          ),
          const SizedBox(height: 20),
        ],
      ),
    );
  }
}
