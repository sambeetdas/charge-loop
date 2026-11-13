import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import 'home_screen.dart';
import 'vendor_home_screen.dart';

class AuthScreen extends StatefulWidget {
  const AuthScreen({Key? key}) : super(key: key);

  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _businessController = TextEditingController();

  String _selectedRole = 'driver'; // 'driver' or 'vendor'
  bool _isLoading = false;

  void _login() async {
    final phone = _phoneController.text.trim().replaceAll(' ', '');
    final name = _nameController.text.trim();
    final business = _businessController.text.trim();

    if (phone.isEmpty || phone.length < 10) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a valid 10-digit mobile number')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final client = ApiClient();

    try {
      if (_selectedRole == 'driver') {
        final profile = await client.authenticate(phone, name: name.isEmpty ? null : name);
        setState(() => _isLoading = false);

        if (mounted) {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (_) => HomeScreen(profile: profile)),
          );
        }
      } else {
        final vendor = await client.authenticateVendor(
          phone,
          name: name.isEmpty ? null : name,
          businessName: business.isEmpty ? null : business,
        );
        setState(() => _isLoading = false);

        if (mounted) {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (_) => VendorHomeScreen(vendor: vendor)),
          );
        }
      }
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(backgroundColor: AppColors.dangerRed, content: Text('Error: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 16),
              // Brand Logo
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: AppColors.neonGreen.withOpacity(0.15),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(Icons.electric_bolt, color: AppColors.neonGreen, size: 28),
                  ),
                  const SizedBox(width: 12),
                  const Text(
                    'ChargeLoop',
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.w900,
                      color: AppColors.textPrimary,
                      letterSpacing: 0.5,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 36),

              const Text(
                'Sign In / Sign Up',
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'Select your account type to access the battery network.',
                style: TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 24),

              // Account Role Selection
              const Text(
                'ACCOUNT TYPE',
                style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: GestureDetector(
                      onTap: () => setState(() => _selectedRole = 'driver'),
                      child: Container(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        decoration: BoxDecoration(
                          color: _selectedRole == 'driver' ? AppColors.neonGreen.withOpacity(0.15) : AppColors.surface,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: _selectedRole == 'driver' ? AppColors.neonGreen : AppColors.border,
                            width: _selectedRole == 'driver' ? 2 : 1,
                          ),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.directions_bike, color: _selectedRole == 'driver' ? AppColors.neonGreen : AppColors.textSecondary, size: 20),
                            const SizedBox(width: 8),
                            Text(
                              'Driver',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: _selectedRole == 'driver' ? Colors.white : AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: GestureDetector(
                      onTap: () => setState(() => _selectedRole = 'vendor'),
                      child: Container(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        decoration: BoxDecoration(
                          color: _selectedRole == 'vendor' ? AppColors.brightCyan.withOpacity(0.15) : AppColors.surface,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: _selectedRole == 'vendor' ? AppColors.brightCyan : AppColors.border,
                            width: _selectedRole == 'vendor' ? 2 : 1,
                          ),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.storefront, color: _selectedRole == 'vendor' ? AppColors.brightCyan : AppColors.textSecondary, size: 20),
                            const SizedBox(width: 8),
                            Text(
                              'Vendor',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: _selectedRole == 'vendor' ? Colors.white : AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),

              // Mobile Number Input
              Container(
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.border),
                ),
                child: TextField(
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Colors.white),
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.phone, color: AppColors.neonGreen),
                    border: InputBorder.none,
                    contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    hintText: 'Enter 10-digit mobile number',
                    hintStyle: TextStyle(color: AppColors.textSecondary, fontSize: 14),
                  ),
                ),
              ),
              const SizedBox(height: 14),

              // Name Input
              Container(
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.border),
                ),
                child: TextField(
                  controller: _nameController,
                  keyboardType: TextInputType.name,
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w500, color: Colors.white),
                  decoration: InputDecoration(
                    prefixIcon: const Icon(Icons.person_outline, color: AppColors.textSecondary),
                    border: InputBorder.none,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    hintText: _selectedRole == 'driver' ? 'Full Name (optional for existing)' : 'Contact Person Full Name',
                    hintStyle: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
                  ),
                ),
              ),

              // Additional Business Name Input for Vendor
              if (_selectedRole == 'vendor') ...[
                const SizedBox(height: 14),
                Container(
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: TextField(
                    controller: _businessController,
                    keyboardType: TextInputType.text,
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w500, color: Colors.white),
                    decoration: const InputDecoration(
                      prefixIcon: Icon(Icons.business, color: AppColors.textSecondary),
                      border: InputBorder.none,
                      contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                      hintText: 'Station or Business Name',
                      hintStyle: TextStyle(color: AppColors.textSecondary, fontSize: 14),
                    ),
                  ),
                ),
              ],

              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _isLoading ? null : _login,
                child: _isLoading
                    ? const SizedBox(
                        height: 22,
                        width: 22,
                        child: CircularProgressIndicator(color: Colors.black, strokeWidth: 2.5),
                      )
                    : Text(
                        _selectedRole == 'driver' ? 'Sign In as Driver' : 'Sign In as Vendor',
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
              ),
              const SizedBox(height: 24),
              const Center(
                child: Text(
                  'ChargeLoop EV Battery Swapping Network',
                  style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
