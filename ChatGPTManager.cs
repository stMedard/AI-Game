using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using System.Threading.Tasks;

public class ChatGPTManager : MonoBehaviour
{
    public OnResponseEvent OnResponse;
    public int maxTokensForResponse;

    [System.Serializable]
    public class OnResponseEvent : UnityEvent<string> { }

    private OpenAIApi openAI;
    private List<ChatMessage> pendingMessages = new List<ChatMessage>(); // Lista wiadomości oczekujących na wysłanie
    private bool isSendingRequest = false; // Flaga określająca, czy obecnie wysyłane jest zapytanie
    private WaitForSeconds delayBetweenRequests = new WaitForSeconds(0.5f); // Opóźnienie między kolejnymi wysyłkami zapytań

    private void Start()
    {
        openAI = new OpenAIApi(APIKeys.OpenAIKey, APIKeys.Organization);

        // Dodaj zdanie powitalne do listy oczekujących wiadomości
        AddWelcomeMessage();
    }

    // Dodaje zdanie powitalne do listy oczekujących wiadomości
    private void AddWelcomeMessage()
    {
        ChatMessage welcomeMessage = new ChatMessage();
        welcomeMessage.Content = "Witaj! Jak mogę Ci dzisiaj pomóc?";
        welcomeMessage.Role = "system";
        pendingMessages.Add(welcomeMessage);

        // Wyślij powitalną wiadomość
        OnResponse.Invoke(welcomeMessage.Content);
    }

    // Metoda wywoływana, gdy użytkownik wprowadza nową wiadomość
    public void OnUserMessageInput(string newText)
    {
        if (!isSendingRequest)
        {
            // Sprawdzamy, czy wiadomość nie jest duplikatem ostatniej wysłanej wiadomości
            if (pendingMessages.Count == 0 || newText != pendingMessages[pendingMessages.Count - 1].Content)
            {
                ChatMessage newMessage = new ChatMessage();
                newMessage.Content = newText;
                newMessage.Role = "user";

                pendingMessages.Add(newMessage); // Dodajemy nową wiadomość do listy oczekujących wiadomości

                // Jeśli nie wysyłamy obecnie żadnego zapytania, to możemy wysłać nowe zapytanie
                StartCoroutine(SendChatGPTRequest());
            }
        }
    }

    // Metoda asynchronicznie wysyła zapytanie do API ChatGPT
    private IEnumerator SendChatGPTRequest()
    {
        if (pendingMessages.Count > 0)
        {
            isSendingRequest = true; // Ustawiamy flagę na true, aby uniknąć wysyłania równoległych zapytań

            CreateChatCompletionRequest request = new CreateChatCompletionRequest();
            request.Messages = pendingMessages;
            request.Model = "gpt-3.5-turbo";
            request.MaxTokens = maxTokensForResponse; // Ograniczenie liczby tokenów

            // Debug.Log("Sending request to ChatGPT API...");

            Task<CreateChatCompletionResponse> task = openAI.CreateChatCompletion(request); // Uruchamiamy asynchroniczne zadanie

            yield return new WaitUntil(() => task.IsCompleted); // Oczekujemy na zakończenie zadania

            if (task.Result.Choices != null && task.Result.Choices.Count > 0)
            {
                var chatResponse = task.Result.Choices[0].Message;
                // Debug.Log("Received response from ChatGPT API: " + chatResponse.Content);
                OnResponse.Invoke(chatResponse.Content);
            }
            else
            {
                Debug.LogWarning("No response received from ChatGPT API.");
            }

            pendingMessages.Clear(); // Czyścimy listę oczekujących wiadomości
            isSendingRequest = false; // Ustawiamy flagę na false, aby zezwolić na wysłanie kolejnego zapytania
        }
    }
}
