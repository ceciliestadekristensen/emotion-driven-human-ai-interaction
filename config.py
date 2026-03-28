# python_backend/config.py

HOST = "127.0.0.1"
PORT = 5005

# If True, use local Ollama LLM for replies.
# If False, only use the baseline rule-based replies from emotion.py.
USE_LLM = True

# Name of the Ollama model to use.
OLLAMA_MODEL = "llama3.2"   # run:  ollama pull llama3.2
