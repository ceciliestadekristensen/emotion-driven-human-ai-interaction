// UIController.cs
// Handles the Unity UI and connects it to the Python backend.
// - Sends user text to PyClient
// - Updates the small status text (aiText)
// - Appends messages to the scrolling chat log (chatText)
// - Updates the cube color + animation via EmotionVisualizer
//
// NOTE:
// This version uses SetEmotion(string emotion, Color targetColor)

using TMPro;
using UnityEngine;

[System.Serializable]
public class AIReply
{
    public string emotion;   // "happy", "sad", "stressed"
    public float score;      // simple confidence / intensity score
    public string reply;     // the natural-language answer from Python
    public float[] color;    // RGB array [r, g, b] in the 0–1 range
}

public class UIController : MonoBehaviour
{
    [Header("Backend connection")]
    public PyClient client;       // TCP client talking to server.py

    [Header("UI elements")]
    public TMP_InputField input;  // where the user types their message
    public TMP_Text aiText;       // short status / latest AI reply at the top
    public TMP_Text chatText;     // full scrolling chat history

    [Header("Visualization")]
    public EmotionVisualizer visualizer;   // controls the animated cube

    void Awake()
    {
        // Make sure the dispatcher exists so Python callbacks can run on the main thread
        UnityMainThreadDispatcher.Instance();

        // Subscribe to incoming JSON messages from Python
        client.OnJsonReceived += OnAI;

        // --- Input field initialisation ---
        // Start with an empty input string
        if (input != null)
        {
            input.text = "";

            // Force English placeholder text
            var placeholder = input.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = "Type your message here...";
            }
        }
    }

    public void OnClickSend()
    {
        string userText = input.text;

        if (!string.IsNullOrWhiteSpace(userText))
        {
            // Temporary status text while waiting for the AI
            aiText.text = "(Thinking...)";

            // Send text to Python server
            client.SendText(userText);

            // Add user's message to the chat log
            AppendChat("You", userText);

            // Clear the input field and keep keyboard focus
            input.text = "";
            input.ActivateInputField();
        }
    }

    void Update()
    {
        // Allow pressing Enter to send the message
        if (input != null &&
            input.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnClickSend();
        }
    }

    private void OnAI(string json)
    {
        Debug.Log("[Unity] OnAI called with json: " + json);

        // Unity UI must be updated on the main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            try
            {
                // Parse JSON message from Python
                var data = JsonUtility.FromJson<AIReply>(json);
                if (data == null)
                {
                    aiText.text = "(Could not read AI reply)";
                    return;
                }

                // Display short AI reply at the top
                aiText.text = data.reply;

                // Add AI reply to the chat scroll view
                AppendChat("AI", data.reply);

                // Default fallback color (neutral grey)
                Color col = new Color(0.7f, 0.7f, 0.7f);

                // If color provided by backend: convert float[] → Unity Color
                if (data.color != null && data.color.Length >= 3)
                {
                    col = new Color(data.color[0], data.color[1], data.color[2]);
                }

                // Tell cube to update emotion + animate pop-in
                visualizer.SetEmotion(data.emotion, col);   // <-- only 2 arguments
            }
            catch
            {
                aiText.text = "(Could not read AI reply)";
            }
        });
    }

        /// <summary>
    /// Adds a new line to the scrolling chat log.
    /// AI messages are left-aligned, user messages are right-aligned
    /// using TextMeshPro rich text tags.
    /// </summary>
    private void AppendChat(string who, string text)
    {
        if (chatText == null) return;

        // Make sure Rich Text is enabled on the ChatText TMP component in the Inspector.
        string formatted;

        if (who == "You")
        {
            // User message on the right
            formatted = $"<align=right><b>You:</b> {text}</align>\n\n";
        }
        else
        {
            // AI message on the left
            formatted = $"<align=left><b>AI:</b> {text}</align>\n\n";
        }

        chatText.text += formatted;
    }
}
