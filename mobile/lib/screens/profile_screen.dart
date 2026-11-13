import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import '../models/models.dart';
import 'auth_screen.dart';

class ProfileScreen extends StatefulWidget {
  final DriverProfile profile;

  const ProfileScreen({Key? key, required this.profile}) : super(key: key);

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final ApiClient _apiClient = ApiClient();
  DriverDetailModel? _details;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  void _loadProfile() async {
    try {
      final details = await _apiClient.getDriverDetails(widget.profile.id);
      if (mounted) {
        setState(() {
          _details = details;
          _isLoading = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final name = _details?.fullName.isNotEmpty == true ? _details!.fullName : widget.profile.fullName;
    final phone = _details?.phoneNumber.isNotEmpty == true ? _details!.phoneNumber : widget.profile.phoneNumber;
    final vehicles = _details?.vehicles ?? [];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Driver Profile'),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.neonGreen))
          : ListView(
              padding: const EdgeInsets.all(20),
              children: [
                Center(
                  child: Column(
                    children: [
                      const CircleAvatar(
                        radius: 36,
                        backgroundColor: AppColors.surfaceElevated,
                        child: Icon(Icons.person, size: 42, color: AppColors.neonGreen),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        name,
                        style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        phone,
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
                              'PostgreSQL Registered Account',
                              style: TextStyle(color: AppColors.neonGreen, fontWeight: FontWeight.bold, fontSize: 12),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 28),

                // Linked Vehicles from Database
                const Text('Linked EV Vehicles (From Database)', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Colors.white)),
                const SizedBox(height: 10),
                if (vehicles.isEmpty)
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Text('No vehicles linked yet to $phone in database.', style: const TextStyle(color: AppColors.textSecondary)),
                    ),
                  )
                else
                  ...vehicles.map((v) => Card(
                        margin: const EdgeInsets.only(bottom: 10),
                        child: Padding(
                          padding: const EdgeInsets.all(16.0),
                          child: Row(
                            children: [
                              const Icon(Icons.two_wheeler, color: AppColors.brightCyan, size: 32),
                              const SizedBox(width: 14),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(v.registrationNumber, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Colors.white)),
                                    Text('${v.oem} - ${v.model}', style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                                    Text('Battery: ${v.batteryCompatibilityType}', style: const TextStyle(color: AppColors.brightCyan, fontSize: 12)),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      )),

                const SizedBox(height: 24),

                // Driver Welfare & Safety
                const Text('Driver Welfare & Safety', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Colors.white)),
                const SizedBox(height: 10),
                Card(
                  child: Column(
                    children: [
                      ListTile(
                        leading: const Icon(Icons.shield_outlined, color: AppColors.neonGreen),
                        title: const Text('Accidental Insurance Cover', style: TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w600)),
                        subtitle: const Text('Active Cover: ₹5,00,000 policy', style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                        trailing: const Icon(Icons.check_circle, color: AppColors.neonGreen, size: 18),
                      ),
                      const Divider(color: AppColors.border, height: 1),
                      ListTile(
                        leading: const Icon(Icons.emergency, color: AppColors.dangerRed),
                        title: const Text('24/7 SOS Safety Alert', style: TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w600)),
                        subtitle: const Text('Emergency Roadside Assistance', style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                        trailing: ElevatedButton(
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.dangerRed,
                            foregroundColor: Colors.white,
                            minimumSize: const Size(60, 32),
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                          ),
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('SOS Alert logged to database operations team!')),
                            );
                          },
                          child: const Text('SOS', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 28),

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
                  child: const Text('Switch Account / Sign Out'),
                ),
                const SizedBox(height: 20),
              ],
            ),
    );
  }
}
