# python_backend/emotion.py
"""
Light-weight emotion classifier and baseline replies.

Uses VADER sentiment to map a text to:
- an emotion label ("happy", "sad", "angry", "stressed", "neutral")
- a confidence score
- a baseline reply in English
- an RGB color used by Unity for the cube

This module works even if the LLM is turned off.
"""

from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer

analyzer = SentimentIntensityAnalyzer()

# Simple baseline replies (used when USE_LLM=False or if LLM fails)
REPLIES = {
    "happy": (
        "That's great to hear. What has made you feel happy today?"
    ),
    "sad": (
        "That sounds really heavy. Would you like to share what's been making you feel sad?"
    ),
    "angry": (
        "That sounds frustrating. Would it help to break it down and look at one small part of it?"
    ),
    "stressed": (
        "That sounds stressful. Could we try to sort your tasks into one small step you could do next?"
    ),
    "neutral": (
        "Thanks for sharing. What would you like to get out of this conversation right now?"
    ),
}

# RGB colors used by Unity for the cube (0–1 range)
COLORS = {
    "happy":   (0.2, 0.8, 0.3),   # green
    "sad":     (0.2, 0.3, 0.8),   # blue
    "angry":   (0.9, 0.2, 0.2),   # red
    "stressed":(0.8, 0.6, 0.2),   # orange
    "neutral": (0.7, 0.7, 0.7),   # grey
}


def classify_emotion(text: str):
    """
    Classify a text into a coarse emotion label plus a simple score.

    We combine:
    - keywords (for "stressed"/"angry" in both Danish + English)
    - VADER compound score for general positivity/negativity
    """
    t = text.lower()
    scores = analyzer.polarity_scores(text)
    comp = scores["compound"]

    # keywords for "stressed"
    if any(w in t for w in ["stress", "stressed", "overwhelmed"]):
        return "stressed", abs(comp)

    # keywords for "angry"
    if any(w in t for w in ["angry", "mad", "annoyed", "irritated"]):
        return "angry", abs(comp)

    # sentiment-based mapping VADER
    if comp >= 0.4:
        return "happy", comp
    if comp <= -0.2:
        return "sad", abs(comp)

    return "neutral", abs(comp)


def baseline_reply(emotion: str) -> str:
    """Return a simple English baseline reply for a given emotion."""
    return REPLIES.get(emotion, REPLIES["neutral"])


def handle_text(text: str, history: list) -> dict:
    """
    Main entry point used by server.py.

    Parameters:
        text: latest user message
        history: list of {"role": "user"/"ai", "content": "..."} (not used
                 here yet, but kept for future extensions).

    Returns:
        dict with keys: emotion, score, reply, color
    """
    emo, score = classify_emotion(text)
    color = COLORS.get(emo, COLORS["neutral"])

    return {
        "emotion": emo,
        "score": float(score),
        "reply": baseline_reply(emo),
        "color": list(color),
    }
