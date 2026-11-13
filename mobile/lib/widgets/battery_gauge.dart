import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';

class BatteryGauge extends StatelessWidget {
  final int percentage;
  final double size;

  const BatteryGauge({
    Key? key,
    required this.percentage,
    this.size = 120.0,
  }) : super(key: key);

  Color get color {
    if (percentage > 50) return AppColors.neonGreen;
    if (percentage > 20) return AppColors.warningAmber;
    return AppColors.dangerRed;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: AppColors.surface,
        border: Border.all(color: color.withOpacity(0.3), width: 3),
        boxShadow: [
          BoxShadow(
            color: color.withOpacity(0.2),
            blurRadius: 16,
            spreadRadius: 2,
          ),
        ],
      ),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.bolt, color: color, size: size * 0.28),
            Text(
              '$percentage%',
              style: TextStyle(
                color: AppColors.textPrimary,
                fontSize: size * 0.22,
                fontWeight: FontWeight.bold,
              ),
            ),
            Text(
              'SOC LEVEL',
              style: TextStyle(
                color: AppColors.textSecondary,
                fontSize: size * 0.08,
                letterSpacing: 1.0,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
