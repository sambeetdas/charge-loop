package com.chargeloop.driver.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.chargeloop.driver.ui.components.BatteryGauge
import com.chargeloop.driver.ui.theme.*
import com.chargeloop.driver.viewmodel.MainViewModel
import com.chargeloop.driver.viewmodel.SwapStep

@Composable
fun SwapScreen(viewModel: MainViewModel) {
    val step by viewModel.swapStep.collectAsState()
    val soc by viewModel.batterySoc.collectAsState()
    val seconds by viewModel.timerSeconds.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(DarkBackground)
            .padding(20.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        when (step) {
            SwapStep.IDLE -> {
                Spacer(modifier = Modifier.height(20.dp))
                BatteryGauge(percentage = soc, size = 140.dp)
                Spacer(modifier = Modifier.height(18.dp))
                Text(
                    text = if (soc <= 20) "Low Battery Warning ($soc%)" else "Battery Status ($soc%)",
                    color = if (soc <= 20) DangerRed else NeonGreen,
                    fontWeight = FontWeight.Bold,
                    fontSize = 18.sp
                )
                Text(
                    text = "Estimated Range: ~${soc * 0.8} km remaining",
                    color = TextSecondary,
                    fontSize = 13.sp
                )
                Spacer(modifier = Modifier.weight(1f))

                Card(
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = SurfaceDark),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(
                        modifier = Modifier.padding(16.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(text = "📍", fontSize = 28.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Column(modifier = Modifier.weight(1f)) {
                            Text(text = "Janakpuri Swap Hub", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                            Text(text = "Operator: Ramesh Kumar (Kirana)", color = TextSecondary, fontSize = 12.sp)
                        }
                        Box(
                            modifier = Modifier
                                .clip(RoundedCornerShape(6.dp))
                                .background(NeonGreen.copy(alpha = 0.15f))
                                .padding(horizontal = 8.dp, vertical = 4.dp)
                        ) {
                            Text(text = "8 Ready", color = NeonGreen, fontWeight = FontWeight.Bold, fontSize = 11.sp)
                        }
                    }
                }

                Spacer(modifier = Modifier.height(20.dp))

                Button(
                    onClick = { viewModel.startSwap() },
                    colors = ButtonDefaults.buttonColors(containerColor = NeonGreen),
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(52.dp)
                ) {
                    Text(text = "⚡ Scan & Initiate 2-Min Swap", color = Color.Black, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                }
            }

            SwapStep.VALIDATING -> {
                Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        CircularProgressIndicator(color = NeonGreen)
                        Spacer(modifier = Modifier.height(16.dp))
                        Text(text = "Handshake in progress (< 1.5s)...", color = TextPrimary, fontWeight = FontWeight.Bold)
                    }
                }
            }

            SwapStep.IN_PROGRESS -> {
                val mins = String.format("%02d", seconds / 60)
                val secs = String.format("%02d", seconds % 60)

                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(18.dp))
                        .background(SurfaceDark)
                        .border(1.dp, BrightCyan.copy(alpha = 0.4f), RoundedCornerShape(18.dp))
                        .padding(20.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text(text = "SWAP TIMER (TARGET: < 2:00)", color = TextSecondary, fontSize = 12.sp, letterSpacing = 1.sp)
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(text = "$mins:$secs", color = BrightCyan, fontSize = 48.sp, fontWeight = FontWeight.Black, fontFamily = FontFamily.Monospace)
                    }
                }

                Spacer(modifier = Modifier.height(20.dp))

                Card(
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = SurfaceDark),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                            Text(text = "Returned Battery:", color = TextSecondary, fontSize = 13.sp)
                            Text(text = "BAT-48V-9999 (16%)", color = WarningAmber, fontWeight = FontWeight.Bold, fontSize = 13.sp)
                        }
                        HorizontalDivider(color = BorderDark, modifier = Modifier.padding(vertical = 10.dp))
                        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                            Text(text = "Issued New Battery:", color = TextSecondary, fontSize = 13.sp)
                            Text(text = "BAT-48V-1001 (100% Full)", color = NeonGreen, fontWeight = FontWeight.Bold, fontSize = 13.sp)
                        }
                    }
                }

                Spacer(modifier = Modifier.weight(1f))

                Button(
                    onClick = { viewModel.completeSwap() },
                    colors = ButtonDefaults.buttonColors(containerColor = NeonGreen),
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(52.dp)
                ) {
                    Text(text = "✓ Confirm Physical Swap Completed", color = Color.Black, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                }
            }

            SwapStep.COMPLETED -> {
                Spacer(modifier = Modifier.height(24.dp))
                Text(text = "✓", fontSize = 54.sp, color = NeonGreen, fontWeight = FontWeight.Black)
                Text(text = "Swap Completed Successfully!", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 20.sp)
                Text(text = "Duration: $seconds seconds", color = NeonGreen, fontSize = 14.sp)

                Spacer(modifier = Modifier.height(20.dp))

                Card(
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = SurfaceDark),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                            Text(text = "Base Swap Fee", color = TextSecondary, fontSize = 13.sp)
                            Text(text = "₹100.00", color = TextPrimary, fontSize = 13.sp)
                        }
                        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, modifier = Modifier.padding(top = 4.dp)) {
                            Text(text = "GST (18%)", color = TextSecondary, fontSize = 13.sp)
                            Text(text = "₹18.00", color = TextPrimary, fontSize = 13.sp)
                        }
                        HorizontalDivider(color = BorderDark, modifier = Modifier.padding(vertical = 8.dp))
                        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                            Text(text = "Total Charged", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                            Text(text = "₹118.00", color = NeonGreen, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                        }
                        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, modifier = Modifier.padding(top = 4.dp)) {
                            Text(text = "Partner Commission", color = TextSecondary, fontSize = 12.sp)
                            Text(text = "₹25.00 Credited", color = BrightCyan, fontSize = 12.sp)
                        }
                    }
                }

                Spacer(modifier = Modifier.weight(1f))

                Button(
                    onClick = { viewModel.resetSwap() },
                    colors = ButtonDefaults.buttonColors(containerColor = NeonGreen),
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(52.dp)
                ) {
                    Text(text = "Back to Home & Resume Driving", color = Color.Black, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                }
            }
        }
    }
}
