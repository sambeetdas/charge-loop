package com.chargeloop.driver.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.chargeloop.driver.ui.theme.*
import com.chargeloop.driver.viewmodel.MainViewModel

@Composable
fun WalletScreen(viewModel: MainViewModel) {
    val balance by viewModel.walletBalance.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(DarkBackground)
            .padding(20.dp)
    ) {
        // Balance Card
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(20.dp))
                .background(
                    Brush.linearGradient(listOf(Color(0xFF1E2836), Color(0xFF141C24)))
                )
                .border(1.dp, BorderDark, RoundedCornerShape(20.dp))
                .padding(24.dp)
        ) {
            Column {
                Text(text = "TOTAL WALLET BALANCE", color = TextSecondary, fontSize = 12.sp, letterSpacing = 1.sp)
                Spacer(modifier = Modifier.height(8.dp))
                Text(text = "₹${String.format("%.2f", balance)}", color = TextPrimary, fontSize = 36.sp, fontWeight = FontWeight.ExtraBold)
                Spacer(modifier = Modifier.height(6.dp))
                Text(text = "✓ Sufficient for approx. 3 battery swaps", color = NeonGreen, fontSize = 13.sp)
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        Text(text = "Quick Top-Up (UPI AutoPay)", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
        Spacer(modifier = Modifier.height(12.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            listOf(100.0, 250.0, 500.0).forEach { amt ->
                OutlinedButton(
                    onClick = { viewModel.topUpWallet(amt) },
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.outlinedButtonColors(containerColor = NeonGreen.copy(alpha = 0.05f)),
                    border = ButtonDefaults.outlinedButtonBorder.copy(brush = Brush.linearGradient(listOf(NeonGreen, NeonGreen))),
                    modifier = Modifier
                        .weight(1f)
                        .height(48.dp)
                ) {
                    Text(text = "+ ₹${amt.toInt()}", color = NeonGreen, fontWeight = FontWeight.Bold)
                }
            }
        }

        Spacer(modifier = Modifier.height(28.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(text = "Recent Transactions", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
            Text(text = "GST Invoices", color = BrightCyan, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        }

        Spacer(modifier = Modifier.height(12.dp))

        Card(
            shape = RoundedCornerShape(14.dp),
            colors = CardDefaults.cardColors(containerColor = SurfaceDark),
            modifier = Modifier.fillMaxWidth()
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(14.dp),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column {
                    Text(text = "Battery Swap Fee", color = TextPrimary, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    Text(text = "Janakpuri Hub • Today", color = TextSecondary, fontSize = 12.sp)
                }
                Text(text = "-₹118.00", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
            }
        }

        Spacer(modifier = Modifier.height(10.dp))

        Card(
            shape = RoundedCornerShape(14.dp),
            colors = CardDefaults.cardColors(containerColor = SurfaceDark),
            modifier = Modifier.fillMaxWidth()
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(14.dp),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column {
                    Text(text = "UPI AutoPay Top-Up", color = TextPrimary, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    Text(text = "Google Pay • Yesterday", color = TextSecondary, fontSize = 12.sp)
                }
                Text(text = "+₹500.00", color = NeonGreen, fontWeight = FontWeight.Bold, fontSize = 15.sp)
            }
        }
    }
}
