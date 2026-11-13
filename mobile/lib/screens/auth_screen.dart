import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';
import '../core/network/api_client.dart';
import 'home_screen.dart';

class AuthScreen extends StatefulWidget {
  const AuthScreen({Key? key}) : super(key: key);

  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  bool _isLoading = false;
  String _selectedLanguage = 'hi';

  void _login() async {
    final phone = _phoneController.text.trim().replaceAll(' ', '');
    final name = _nameController.text.trim();

    if (phone.isEmpty || phone.length < 10) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a valid 10-digit mobile number')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final client = ApiClient();
    try {
      final profile = await client.authenticate(phone, name: name.isEmpty ? null : name);
      setState(() => _isLoading = false);

      if (mounted) {
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(builder: (_) => HomeScreen(profile: profile)),
        );
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
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 20),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
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
                  // Vernacular Language Switcher
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: _selectedLanguage,
                        dropdownColor: AppColors.surface,
                        items: const [
                          DropdownMenuItem(value: 'hi', child: Text('🇮🇳 हिन्दी')),
                          DropdownMenuItem(value: 'en', child: Text('🇬🇧 English')),
                        ],
                        onChanged: (val) {
                          if (val != null) setState(() => _selectedLanguage = val);
                        },
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 48),
              Text(
                _selectedLanguage == 'hi' ? 'ड्राइवर लॉगिन' : 'Driver Sign In',
                style: const TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                _selectedLanguage == 'hi'
                    ? 'अपना मोबाइल नंबर दर्ज करें और नेटवर्क से जुड़ें।'
                    : 'Enter your phone number to access your battery network.',
                style: const TextStyle(
                  fontSize: 15,
                  color: AppColors.textSecondary,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 32),
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
                  style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600, color: Colors.white),
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.phone, color: AppColors.neonGreen),
                    border: InputBorder.none,
                    contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    hintText: 'Enter 10-digit mobile number',
                    hintStyle: TextStyle(color: AppColors.textSecondary, fontSize: 15),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              // Full Name Input (Optional if already registered in DB)
              Container(
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.border),
                ),
                child: TextField(
                  controller: _nameController,
                  keyboardType: TextInputType.name,
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w500, color: Colors.white),
                  decoration: const InputDecoration(
                    prefixIcon: Icon(Icons.person_outline, color: AppColors.textSecondary),
                    border: InputBorder.none,
                    contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    hintText: 'Full Name (optional for existing drivers)',
                    hintStyle: TextStyle(color: AppColors.textSecondary, fontSize: 14),
                  ),
                ),
              ),
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
                        _selectedLanguage == 'hi' ? 'आगे बढ़ें (Verify & Login)' : 'Verify & Continue',
                        style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
                      ),
              ),
              const SizedBox(height: 20),
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
