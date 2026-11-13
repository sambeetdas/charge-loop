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
  final TextEditingController _phoneController = TextEditingController(text: '+91 98765 00001');
  bool _isLoading = false;
  String _selectedLanguage = 'hi';

  void _login() async {
    setState(() => _isLoading = true);
    final client = ApiClient();
    final profile = await client.authenticate(_phoneController.text.replaceAll(' ', ''));
    setState(() => _isLoading = false);

    if (mounted) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => HomeScreen(profile: profile)),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Padding(
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
              const Spacer(),
              Text(
                _selectedLanguage == 'hi' ? 'नमस्ते ड्राइवर साथी!' : 'Welcome Driver Partner!',
                style: const TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                _selectedLanguage == 'hi'
                    ? '2 मिनट में बैटरी बदलें और बिना रुके कमाई करें।'
                    : 'Swap your EV battery in 2 minutes & stay on the road.',
                style: const TextStyle(
                  fontSize: 15,
                  color: AppColors.textSecondary,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 32),
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
                    hintText: 'Enter Mobile Number',
                    hintStyle: TextStyle(color: AppColors.textSecondary),
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
                        _selectedLanguage == 'hi' ? 'OTP प्राप्त करें व आगे बढ़ें' : 'Send OTP & Continue',
                        style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
                      ),
              ),
              const SizedBox(height: 16),
              const Center(
                child: Text(
                  'By signing in, you agree to DPDP Act 2023 privacy terms',
                  style: TextStyle(fontSize: 11, color: AppColors.textSecondary),
                ),
              ),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}
