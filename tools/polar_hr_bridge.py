#!/usr/bin/env python3
"""
Polar H10/H9 -> Unity UDP bridge.

Usage:
  pip install bleak
  python tools/polar_hr_bridge.py

Unity listens on UDP port 7878 by default.
"""

from __future__ import annotations

import asyncio
import socket
import sys

try:
    from bleak import BleakClient, BleakScanner
except ImportError:
    print("Install bleak first: pip install bleak")
    sys.exit(1)

HR_SERVICE = "0000180d-0000-1000-8000-00805f9b34fb"
HR_MEASUREMENT = "00002a37-0000-1000-8000-00805f9b34fb"
UDP_HOST = "127.0.0.1"
UDP_PORT = 7878


def parse_hr(data: bytearray) -> int:
    flags = data[0]
    if flags & 0x01:
        return int(data[1]) + (int(data[2]) << 8)
    return int(data[1])


async def main() -> None:
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print("Scanning for Polar heart rate sensor...")

    device = await BleakScanner.find_device_by_filter(
        lambda d, ad: d.name and "Polar" in d.name,
        timeout=20.0,
    )

    if device is None:
        print("No Polar device found. Wet the strap electrodes and wear it snugly.")
        return

    print(f"Found {device.name}. Connecting...")

    def on_hr(_handle, data: bytearray) -> None:
        bpm = parse_hr(data)
        message = f"BPM:{bpm}".encode("utf-8")
        sock.sendto(message, (UDP_HOST, UDP_PORT))
        print(f"BPM: {bpm}")

    async with BleakClient(device) as client:
        await client.start_notify(HR_MEASUREMENT, on_hr)
        print(f"Streaming to Unity at {UDP_HOST}:{UDP_PORT}. Press Ctrl+C to stop.")
        while True:
            await asyncio.sleep(1.0)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nStopped.")
