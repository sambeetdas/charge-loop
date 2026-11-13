package com.chargeloop.driver.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ElectricBolt
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.chargeloop.driver.ui.theme.*

@Composable
fun BatteryGauge(
    percentage: Int,
    size: Dp = 130.dp,
    modifier: Modifier = Modifier
) {
    val color = when {
        percentage > 50 -> NeonGreen
        percentage > 20 -> WarningAmber
        else -> DangerRed
    }

    Box(
        modifier = modifier
            .size(size)
            .clip(CircleShape)
            .background(SurfaceDark)
            .border(3.dp, color.copy(alpha = 0.5f), CircleShape),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Icon(
                imageVector = Icons.Default.ElectricBolt,
                contentDescription = null,
                tint = color,
                modifier = Modifier.size(32.dp)
            )
            Text(
                text = "$percentage%",
                color = TextPrimary,
                fontSize = 28.sp,
                fontWeight = FontWeight.ExtraBold
            )
            Text(
                text = "SOC LEVEL",
                color = TextSecondary,
                fontSize = 10.sp,
                letterSpacing = 1.sp
            )
        }
    }
}
