import asyncio
import websockets
import json

PORT = 8080

async def send_speak(ws, intent, text):
    msg = {
        "intent": intent,   # "high", "medium", "low"
        "text": text
    }
    await ws.send(json.dumps(msg))

async def handler(websocket):
    print("Quest connected")

    # Send one calm message when Quest connects
    await send_speak(websocket, "high", "Welcome. This is a very calm message.")

    step = 0
    try:
        while True:
            # cycle through intents every 5 seconds
            if step == 0:
                await send_speak(websocket, "medium",
                                 "This is a moderately calm instruction.")
            elif step == 1:
                await send_speak(websocket, "low",
                                 "This is a normal tone message.")
            elif step == 2:
                await send_speak(websocket, "high",
                                 "Please take a deep breath and relax.")
                step = -1  # wrap

            step += 1

            # non-blocking small receive (optional)
            try:
                msg = await asyncio.wait_for(websocket.recv(), timeout=0.1)
                print("From Quest:", msg)
            except asyncio.TimeoutError:
                pass

            await asyncio.sleep(5.0)

    except websockets.exceptions.ConnectionClosed:
        print("Quest disconnected")

async def main():
    async with websockets.serve(handler, "0.0.0.0", PORT):
        print(f"WebSocket server listening on ws://0.0.0.0:{PORT}")
        await asyncio.Future()  # run forever

asyncio.run(main())