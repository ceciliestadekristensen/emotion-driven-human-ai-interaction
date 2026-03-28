# python_backend/llm.py
"""
Thin wrapper around a local Ollama HTTP API call.

This file deliberately does NOT know anything about prompts,
emotions or history. It just sends a prompt string and returns
the raw text response.
"""

import requests
import simplejson as json


def call_ollama(model: str, prompt: str,
                url: str = "http://127.0.0.1:11434/api/generate") -> str:
    """
    Call a local Ollama model and return the response text.

    Parameters:
        model: name of the Ollama model (e.g. "llama3.2")
        prompt: full text prompt
        url: Ollama HTTP endpoint

    Returns:
        The 'response' field from Ollama as a stripped string.
    """
    payload = {"model": model, "prompt": prompt, "stream": False}
    r = requests.post(url, json=payload, timeout=60)
    r.raise_for_status()
    data = r.json()
    return data.get("response", "").strip()
