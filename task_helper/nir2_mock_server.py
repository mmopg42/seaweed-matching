"""
NIR2 Mock HTTP Server for testing data collection.

This server simulates the NIR2 device HTTP API at http://127.0.0.1:10024/1
It returns JSON data matching the structure expected by the data collector.

Presence value (1 or 2) is generated with constrained streak pattern:
- 1 appears in streaks of 2-4 consecutive requests (minimum 2, maximum 4)
  Distribution: 2 consecutive: 25%, 3 consecutive: 50%, 4 consecutive: 25%
- 2 appears in streaks of 1-4 consecutive requests (minimum 1, maximum 4)
  Distribution: Equal probability (25% each)

Usage:
    python task_helper/nir2_mock_server.py

The server will run until Ctrl+C is pressed.
"""

from http.server import HTTPServer, BaseHTTPRequestHandler
import json
from datetime import datetime
import random


class Nir2MockHandler(BaseHTTPRequestHandler):
    """HTTP request handler that mocks NIR2 device responses."""

    request_count = 0

    # Presence streak state (shared across all instances)
    current_streak_remaining = 0
    current_presence = 2

    # Distribution for 1-streak: 2-4 consecutive (25%, 50%, 25%)
    STREAK_LENGTHS_1 = [2, 3, 3, 4]

    # Distribution for 2-streak: 2-4 consecutive (equal probability)
    STREAK_LENGTHS_2 = [2, 3, 3, 4]

    def _set_headers(self, status_code=200):
        """Set response headers."""
        self.send_response(status_code)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()

    def do_GET(self):
        """Handle GET requests."""
        if self.path == '/1':
            self._send_nir2_data()
        else:
            self._set_headers(404)
            self.wfile.write(b'{"error": "Not found"}')

    def _generate_timestamp(self):
        """Generate current timestamp in ISO format with timezone."""
        now = datetime.now()
        ts = now.strftime('%Y-%m-%dT%H:%M:%S.%f')[:-2]
        return f"{ts}+0900"

    def _get_presence(self):
        """Generate presence value with constrained streak pattern.

        Returns 1 or 2 with the following rules:
        - 1 appears in streaks of 2-4 consecutive requests
        - 2 appears in streaks of 1-4 consecutive requests
        """
        # If we have remaining streak, continue
        if Nir2MockHandler.current_streak_remaining > 0:
            Nir2MockHandler.current_streak_remaining -= 1
            return Nir2MockHandler.current_presence

        # Start a new streak (previous streak just ended)
        # Switch to the other presence value and pick a new streak length
        if Nir2MockHandler.current_presence == 1:
            # Switch to 2-streak
            Nir2MockHandler.current_presence = 2
            # remaining = streak_length - 1 (this call counts as 1)
            Nir2MockHandler.current_streak_remaining = random.choice(
                Nir2MockHandler.STREAK_LENGTHS_2
            ) - 1
        else:
            # Switch to 1-streak
            Nir2MockHandler.current_presence = 1
            # remaining = streak_length - 1 (this call counts as 1)
            Nir2MockHandler.current_streak_remaining = random.choice(
                Nir2MockHandler.STREAK_LENGTHS_1
            ) - 1

        return Nir2MockHandler.current_presence

    def _send_nir2_data(self):
        """Generate and send mock NIR2 data."""
        Nir2MockHandler.request_count += 1

        protein = 40.0 + random.uniform(-0.5, 0.5)
        moisture = 10.0 + random.uniform(-0.2, 0.2)
        presence = self._get_presence()

        timestamp = self._generate_timestamp()

        Nir2MockHandler.request_count += 1
        seq = Nir2MockHandler.request_count

        response_data = {
            "Batch": {
                "End": timestamp
            },
            "Parameters": [
                {"Name": "Protein", "Value": round(protein, 4)},
                {"Name": "Moisture", "Value": round(moisture, 4)},
                {"Name": "Presence", "Value": presence}
            ]
        }

        self._set_headers(200)
        self.wfile.write(json.dumps(response_data).encode('utf-8'))

    def log_message(self, format, *args):
        pass  # Suppress default logging


def run_server(port=10024):
    """Start the mock HTTP server."""
    server_address = ('', port)
    httpd = HTTPServer(server_address, Nir2MockHandler)

    print("NIR2 Mock Server running at http://127.0.0.1:{}/1".format(port))
    print("Press Ctrl+C to stop")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down...")
        httpd.shutdown()


if __name__ == '__main__':
    run_server()
