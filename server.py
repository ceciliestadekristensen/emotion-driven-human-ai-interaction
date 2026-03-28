# python_backend/server.py
"""
Simple TCP server that connects Unity and Python.

Flow:
- Unity sends JSON: {"text": "<user message>"}
- We classify emotion + color with VADER (emotion.py)
- Optionally call a local LLM via Ollama for a natural-language reply
- We send JSON back: {"emotion": "...", "score": float, "reply": "...", "color": [r,g,b]}
"""

import socket
import threading
import simplejson as json
from typing import List, Dict

from config import HOST, PORT, USE_LLM, OLLAMA_MODEL
from emotion import handle_text               # classical NLP (VADER) for emotion + color + baseline reply
from llm import call_ollama                   # optional LLM call when USE_LLM = True


# System prompt that controls the LLM's style (only used when USE_LLM = True)
SYSTEM_PROMPT = """
You are a warm, empathetic English-speaking companion.

RULES:
- Always answer in English.
- Keep replies short and clear (1–3 sentences).
- Do NOT guess specific life events (do not assume things like family, work, school or relationships unless the user mentions them).
- First acknowledge the user's feelings in a simple way.
- Then ask one gentle, open question or suggest one small next step.
- No emojis (Unity handles visuals separately).
- Avoid slang. Use calm, everyday English.

Examples:
User: "I'm stressed"
AI: "That sounds really tough. What do you think is the main thing that is stressing you at the moment?"

User: "I'm happy today"
AI: "I'm glad to hear that. What happened today that made you feel happy?"

User: "I don't know how I feel"
AI: "That's completely okay. Feelings can be confusing. Could you tell me a bit about what your day has been like?"
""".strip()


# ---------------------------------------------------------------------------
# Small helper: remove outer quotes from the reply text.
#
# Many LLMs sometimes answer like:
#   "That sounds lovely! What brought a smile to your face today?"
# We want Unity to just display:
#   That sounds lovely! What brought a smile to your face today?
# ---------------------------------------------------------------------------
def strip_outer_quotes(text: str) -> str:
    """Remove single/double quotes around the whole reply, if they exist."""
    if not text:
        return text
    t = text.strip()
    if len(t) >= 2 and (
        (t[0] == '"' and t[-1] == '"') or
        (t[0] == "'" and t[-1] == "'")
    ):
        t = t[1:-1].strip()
    return t


def build_prompt(emotion: str, history: List[Dict[str, str]], user_text: str) -> str:
    """
    Builds the full prompt for the LLM.

    - emotion: label from VADER ("happy", "sad", ...)
    - history: list of {"role": "user"/"ai", "content": "..."} to give context
    - user_text: latest user message
    """
    hist_lines = []
    for msg in history[-6:]:  # only send the last few messages for context
        who = "User" if msg["role"] == "user" else "AI"
        hist_lines.append(f"{who}: {msg['content']}")
    hist_block = "\n".join(hist_lines) if hist_lines else "(no previous messages)"

    return f"""{SYSTEM_PROMPT}

Conversation so far:
{hist_block}

Current emotion hint: {emotion}

User: {user_text}
AI:"""


def handle_message(text: str, history: List[Dict[str, str]]) -> Dict:
    """
    High-level message handler used by the socket thread.

    Steps:
    1) Use VADER-based classifier to get emotion + score + baseline reply + color.
    2) Append user message to conversation history.
    3) If USE_LLM is True: call LLM to get a richer reply (otherwise use baseline).
    4) Append AI reply to history.
    5) Return a compact JSON-safe dict for Unity.
    """
    # Step 1: classical emotion analysis
    base = handle_text(text, history)  # {"emotion", "score", "reply", "color"}

    # Clean up baseline reply in case emotion.py ever adds quotes
    base_reply = strip_outer_quotes(base.get("reply", ""))

    # Step 2: log user message
    history.append({"role": "user", "content": text})

    # Step 3: optionally call LLM
    reply_text = base_reply
    if USE_LLM:
        try:
            prompt = build_prompt(base["emotion"], history, text)
            llm_reply = call_ollama(OLLAMA_MODEL, prompt) or base_reply
            # Remove outer quotes if the model wrapped the whole reply in "" or ''
            reply_text = strip_outer_quotes(llm_reply)
        except Exception as e:
            print("LLM ERROR, falling back to baseline reply:", e)
            reply_text = base_reply

    # Step 4: log AI reply
    history.append({"role": "ai", "content": reply_text})

    # Step 5: send compact payload to Unity
    return {
        "emotion": base["emotion"],
        "score": float(base["score"]),
        "reply": reply_text,
        "color": list(base["color"]),
    }


def client_thread(conn: socket.socket, addr):
    """Handles a single TCP client (Unity). Runs in its own thread."""
    print("CLIENT CONNECTED:", addr)
    history: List[Dict[str, str]] = []

    with conn:
        while True:
            data = conn.recv(8192)
            if not data:
                break

            print("RECV BYTES:", len(data))
            try:
                msg = json.loads(data.decode("utf-8"))
                print("RECV JSON:", msg)
                text = msg.get("text", "")
            except Exception as e:
                print("JSON ERROR:", e)
                text = data.decode("utf-8", errors="ignore")

            out = handle_message(text, history)
            print("SEND JSON:", out)

            payload = json.dumps(out).encode("utf-8")
            conn.sendall(payload)

    print("CLIENT DISCONNECTED:", addr)


def main():
    """Entry point: start TCP server and keep accepting Unity connections."""
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        # allow quick restart while debugging
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        s.bind((HOST, PORT))
        s.listen()
        print(f"[Python] Listening on {HOST}:{PORT} (USE_LLM={USE_LLM})")

        while True:
            conn, addr = s.accept()
            t = threading.Thread(target=client_thread, args=(conn, addr), daemon=True)
            t.start()


if __name__ == "__main__":
    main()
