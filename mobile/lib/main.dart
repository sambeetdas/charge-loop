import 'package:flutter/material.dart';
import 'core/theme/app_theme.dart';
import 'screens/auth_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const ChargeLoopApp());
}

class ChargeLoopApp extends StatelessWidget {
  const ChargeLoopApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ChargeLoop EV Swapping',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.darkTheme,
      home: const AuthScreen(),
    );
  }
}
