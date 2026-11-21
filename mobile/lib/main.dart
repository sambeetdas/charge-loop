import 'package:flutter/material.dart';
import 'package:webview_flutter/webview_flutter.dart';

const String _appUrl = String.fromEnvironment(
  'APP_URL',
  defaultValue: 'http://13.233.63.106/?app=1',
);

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const ChargeLoopApp());
}

class ChargeLoopApp extends StatelessWidget {
  const ChargeLoopApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ChargeLoop',
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark().copyWith(
        scaffoldBackgroundColor: const Color(0xFF0F273B),
      ),
      home: const ChargeLoopWebViewScreen(),
    );
  }
}

class ChargeLoopWebViewScreen extends StatefulWidget {
  const ChargeLoopWebViewScreen({super.key});

  @override
  State<ChargeLoopWebViewScreen> createState() => _ChargeLoopWebViewScreenState();
}

class _ChargeLoopWebViewScreenState extends State<ChargeLoopWebViewScreen> {
  late final WebViewController _controller;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setBackgroundColor(const Color(0xFF0F273B))
      ..setNavigationDelegate(
        NavigationDelegate(
          onPageStarted: (_) {
            if (mounted) {
              setState(() {
                _isLoading = true;
                _errorMessage = null;
              });
            }
          },
          onPageFinished: (_) {
            if (mounted) {
              setState(() => _isLoading = false);
            }
          },
          onWebResourceError: (error) {
            if (error.isForMainFrame == true && mounted) {
              setState(() {
                _isLoading = false;
                _errorMessage = error.description;
              });
            }
          },
        ),
      )
      ..loadRequest(Uri.parse(_appUrl));
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) async {
        if (didPop) return;
        if (await _controller.canGoBack()) {
          await _controller.goBack();
        }
      },
      child: Scaffold(
        backgroundColor: const Color(0xFF0F273B),
        body: SafeArea(
          child: Stack(
            children: [
              WebViewWidget(controller: _controller),
              if (_isLoading)
                const Center(
                  child: CircularProgressIndicator(color: Color(0xFF1AD1A0)),
                ),
              if (_errorMessage != null)
                Container(
                  color: const Color(0xFF0F273B),
                  padding: const EdgeInsets.all(24),
                  child: Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.wifi_off, color: Color(0xFF1AD1A0), size: 52),
                        const SizedBox(height: 16),
                        const Text(
                          'Unable to reach ChargeLoop',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          _errorMessage!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: Color(0xFF8FB3C9), fontSize: 13),
                        ),
                        const SizedBox(height: 20),
                        ElevatedButton(
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFF1AD1A0),
                            foregroundColor: Colors.black,
                          ),
                          onPressed: () => _controller.loadRequest(Uri.parse(_appUrl)),
                          child: const Text('Retry'),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
