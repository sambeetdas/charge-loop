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
  final TextEditingController _pinController = TextEditingController();
  final TextEditingController _confirmPinController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _businessController = TextEditingController();

  String _selectedRole = 'driver'; // 'driver' or 'vendor'
  String _authMode = 'signin'; // 'signin' | 'signup' | 'reset'
  bool _isLoading = false;

  void _submit() async {
    final phone = _phoneController.text.trim().replaceAll(' ', '');
    final pin = _pinController.text.trim();
    final confirmPin = _confirmPinController.text.trim();
    final name = _nameController.text.trim();
    final business = _businessController.text.trim();

    if (phone.isEmpty || phone.length < 10) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a valid 10-digit mobile number')),
      );
      return;
    }

    if (pin.length < 4 || pin.length > 6) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('PIN must be 4 to 6 digits')),
      );
      return;
    }

    if ((_authMode == 'signup' || _authMode == 'reset') && pin != confirmPin) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('PIN and Confirm PIN do not match')),
      );
      return;
    }

    setState(() => _isLoading = true);
    final client = ApiClient();

    try {
      if (_authMode == 'reset') {
        if (_selectedRole == 'driver') {
          await client.resetDriverPin(phone: phone, newPin: pin, confirmNewPin: confirmPin);
        } else {
          await client.resetVendorPin(phone: phone, newPin: pin, confirmNewPin: confirmPin);
        }
        setState(() {
          _isLoading = false;
          _authMode = 'signin';
          _pinController.clear();
          _confirmPinController.clear();
        });
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('PIN reset successfully! Please Sign In.')),
          );
        }
        return;
      }

      if (_selectedRole == 'driver') {
        final profile = _authMode == 'signup'
            ? await client.signUpDriverWithPin(
                phone: phone,
                pin: pin,
                confirmPin: confirmPin,
                fullName: name.isEmpty ? null : name,
              )
            : await client.signInDriverWithPin(phone: phone, pin: pin);
        setState(() => _isLoading = false);

        if (mounted) {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (_) => HomeScreen(profile: profile)),
          );
        }
      } else {
        final vendor = _authMode == 'signup'
            ? await client.signUpVendorWithPin(
                phone: phone,
                pin: pin,
                confirmPin: confirmPin,
                fullName: name.isEmpty ? null : name,
                businessName: business.isEmpty ? null : business,
              )
            : await client.signInVendorWithPin(phone: phone, pin: pin);
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
              // Brand Hero Card
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [
                      AppColors.surfaceElevated,
                      AppColors.surface,
                    ],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: AppColors.neonGreen.withOpacity(0.25)),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.4),
                      blurRadius: 20,
                      offset: const Offset(0, 10),
                    ),
                  ],
                ),
                child: Column(
                  children: [
                    // Official Circular Logo Icon
                    Container(
                      width: 64,
                      height: 64,
                      decoration: BoxDecoration(
                        color: const Color(0xFF00C853),
                        borderRadius: BorderRadius.circular(16),
                        boxShadow: [
                          BoxShadow(
                            color: const Color(0xFF00C853).withOpacity(0.4),
                            blurRadius: 18,
                            spreadRadius: 2,
                          ),
                        ],
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(16),
                        child: Image.asset(
                          'assets/images/logo_icon.png',
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => const Icon(Icons.bolt, color: Colors.amber, size: 36),
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Tagline
                    RichText(
                      textAlign: TextAlign.center,
                      text: const TextSpan(
                        text: 'Swap. Ride. ',
                        style: TextStyle(
                          fontSize: 24,
                          fontWeight: FontWeight.w900,
                          color: Colors.white,
                          letterSpacing: -0.4,
                        ),
                        children: [
                          TextSpan(
                            text: 'Repeat.',
                            style: TextStyle(color: AppColors.neonGreen),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'SMART EV BATTERY SWAPPING NETWORK',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.0,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Feature Assurance Chips
                    Wrap(
                      spacing: 6,
                      runSpacing: 6,
                      alignment: WrapAlignment.center,
                      children: [
                        _buildFeaturePill('⚡ Quick swaps', AppColors.neonGreen),
                        _buildFeaturePill('🔋 Tested batteries', AppColors.brightCyan),
                        _buildFeaturePill('📋 Fleet plans', AppColors.warningAmber),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              const Text(
                'Sign In / Sign Up',
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 4),
              const Text(
                'Select your account type to access the battery network.',
                style: TextStyle(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 20),

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
              const SizedBox(height: 16),

              // Mode Selector (Sign In | Sign Up | Reset PIN)
              Row(
                children: [
                  for (final m in const [
                    ('signin', 'Sign In'),
                    ('signup', 'Sign Up'),
                    ('reset', 'Reset PIN'),
                  ])
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 3),
                        child: GestureDetector(
                          onTap: () => setState(() => _authMode = m.$1),
                          child: Container(
                            padding: const EdgeInsets.symmetric(vertical: 10),
                            decoration: BoxDecoration(
                              color: _authMode == m.$1 ? AppColors.neonGreen.withOpacity(0.16) : AppColors.surface,
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: _authMode == m.$1 ? AppColors.neonGreen : AppColors.border,
                              ),
                            ),
                            child: Text(
                              m.$2,
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: _authMode == m.$1 ? Colors.white : AppColors.textSecondary,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 18),

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

              // PIN Input
              Container(
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.border),
                ),
                child: TextField(
                  controller: _pinController,
                  obscureText: true,
                  keyboardType: TextInputType.number,
                  maxLength: 6,
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Colors.white),
                  decoration: InputDecoration(
                    counterText: '',
                    prefixIcon: const Icon(Icons.lock_outline, color: AppColors.neonGreen),
                    border: InputBorder.none,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    hintText: _authMode == 'reset' ? 'New PIN (4-6 digits)' : 'PIN (4-6 digits)',
                    hintStyle: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
                  ),
                ),
              ),

              if (_authMode == 'signup' || _authMode == 'reset') ...[
                const SizedBox(height: 14),
                Container(
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: TextField(
                    controller: _confirmPinController,
                    obscureText: true,
                    keyboardType: TextInputType.number,
                    maxLength: 6,
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Colors.white),
                    decoration: InputDecoration(
                      counterText: '',
                      prefixIcon: const Icon(Icons.lock_reset, color: AppColors.neonGreen),
                      border: InputBorder.none,
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                      hintText: _authMode == 'reset' ? 'Confirm New PIN' : 'Confirm PIN',
                      hintStyle: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
                    ),
                  ),
                ),
              ],

              if (_authMode == 'signup') ...[
                const SizedBox(height: 14),
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
                      hintText: _selectedRole == 'driver' ? 'Full Name (optional)' : 'Contact Person Full Name',
                      hintStyle: const TextStyle(color: AppColors.textSecondary, fontSize: 14),
                    ),
                  ),
                ),
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
                        hintText: 'Vendor / Enterprise Name (optional)',
                        hintStyle: TextStyle(color: AppColors.textSecondary, fontSize: 14),
                      ),
                    ),
                  ),
                ],
              ],

              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _isLoading ? null : _submit,
                child: _isLoading
                    ? const SizedBox(
                        height: 22,
                        width: 22,
                        child: CircularProgressIndicator(color: Colors.black, strokeWidth: 2.5),
                      )
                    : Text(
                        _authMode == 'reset'
                            ? 'Reset PIN'
                            : (_authMode == 'signup'
                                ? (_selectedRole == 'driver' ? 'Sign Up as Driver' : 'Sign Up as Vendor')
                                : (_selectedRole == 'driver' ? 'Sign In as Driver' : 'Sign In as Vendor')),
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
              ),
              const SizedBox(height: 20),
              Center(
                child: Column(
                  children: [
                    const Text(
                      'Swap. Ride. Repeat.',
                      style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: AppColors.neonGreen),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'www.chargeloopsln.in',
                      style: TextStyle(fontSize: 12, color: AppColors.brightCyan.withOpacity(0.8), decoration: TextDecoration.underline),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildFeaturePill(String label, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withOpacity(0.35)),
      ),
      child: Text(
        label,
        style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold),
      ),
    );
  }
}
