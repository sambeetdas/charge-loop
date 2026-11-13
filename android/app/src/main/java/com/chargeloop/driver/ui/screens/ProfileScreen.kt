package com.chargeloop.driver.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.chargeloop.driver.ui.theme.*
import com.chargeloop.driver.viewmodel.MainViewModel

@Composable
fun ProfileScreen(viewModel: MainViewModel) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(DarkBackground)
            .padding(20.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(
            modifier = Modifier
                .size(72.dp)
                .clip(CircleShape)
                .background(SurfaceElevated),
            contentAlignment = Alignment.Center
        ) {
            Icon(imageVector = Icons.Default.Person, contentDescription = null, tint = NeonGreen, modifier = Modifier.size(40.dp))
        }

        Spacer(modifier = Modifier.height(10.dp))
        Text(text = "Ravi Sharma", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 20.sp)
        Text(text = "+91 98765 00001", color = TextSecondary, fontSize = 13.sp)

        Spacer(modifier = Modifier.height(8.dp))

        Box(
            modifier = Modifier
                .clip(RoundedCornerShape(12.dp))
                .background(NeonGreen.copy(alpha = 0.15f))
                .border(1.dp, NeonGreen, RoundedCornerShape(12.dp))
                .padding(horizontal = 10.dp, vertical = 4.dp)
        ) {
            Text(text = "✓ KYC & Parivahan Verified", color = NeonGreen, fontWeight = FontWeight.Bold, fontSize = 11.sp)
        }

        Spacer(modifier = Modifier.height(24.dp))

        Column(modifier = Modifier.fillMaxWidth(), horizontalAlignment = Alignment.Start) {
            Text(text = "Linked EV Vehicle", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
            Spacer(modifier = Modifier.height(8.dp))
            Card(
                shape = RoundedCornerShape(14.dp),
                colors = CardDefaults.cardColors(containerColor = SurfaceDark),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Text(text = "DL 01 AB 1234", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
                    Text(text = "Quantum Energy - Plaza EV 2W", color = TextSecondary, fontSize = 12.sp)
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(text = "Compatible: 2W_48V Battery", color = BrightCyan, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                }
            }

            Spacer(modifier = Modifier.height(20.dp))

            Text(text = "Driver Welfare & Safety", color = TextPrimary, fontWeight = FontWeight.Bold, fontSize = 15.sp)
            Spacer(modifier = Modifier.height(8.dp))
            Card(
                shape = RoundedCornerShape(14.dp),
                colors = CardDefaults.cardColors(containerColor = SurfaceDark),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(text = "Welfare Fund Insurance", color = TextPrimary, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                            Text(text = "Active: ₹5,00,000 accidental cover", color = TextSecondary, fontSize = 11.sp)
                        }
                        Text(text = "🛡️", fontSize = 20.sp)
                    }

                    HorizontalDivider(color = BorderDark, modifier = Modifier.padding(vertical = 10.dp))

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(text = "SOS Helpline", color = DangerRed, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                            Text(text = "24/7 Roadside & Battery Safety", color = TextSecondary, fontSize = 11.sp)
                        }
                        Button(
                            onClick = { /* SOS trigger */ },
                            colors = ButtonDefaults.buttonColors(containerColor = DangerRed),
                            shape = RoundedCornerShape(8.dp),
                            contentPadding = PaddingValues(horizontal = 14.dp, vertical = 6.dp)
                        ) {
                            Text(text = "SOS", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }
}
