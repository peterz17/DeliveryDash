using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }
    public static event Action OnAuthStateChanged;
    public static event Action OnAuthSessionExpired;

    public string UserId { get; private set; } = "";
    public string IdToken { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public AuthProviderType Provider { get; private set; } = AuthProviderType.Guest;
    public bool IsAuthenticated => !string.IsNullOrEmpty(IdToken);

    // Set your Firebase Web API Key here
    const string FIREBASE_API_KEY = "AIzaSyBvvguczK4SyTild97wO4s31F_y9lmjvgw";
    const string AUTH_BASE = "https://identitytoolkit.googleapis.com/v1/accounts";
    const string TOKEN_BASE = "https://securetoken.googleapis.com/v1/token";
    const float TOKEN_REFRESH_INTERVAL = 50f * 60f; // 50 minutes

    string _refreshToken = "";
    Coroutine _refreshCoroutine;

#if UNITY_EDITOR
    [Header("Editor Dev — paste refresh token from WebGL localStorage")]
    [SerializeField] string editorDevRefreshToken = "";
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        string savedRefresh = PlayerPrefs.GetString("AuthRefreshToken", "");
        string savedProvider = PlayerPrefs.GetString("AuthProvider", "");
        if (!string.IsNullOrEmpty(savedRefresh))
        {
            _refreshToken = savedRefresh;
            if (Enum.TryParse(savedProvider, out AuthProviderType p)) Provider = p;
            StartCoroutine(RefreshTokenCoroutine(null, null));
        }
    }

    public void SignInAnonymous(Action onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(AnonymousAuthCoroutine(onSuccess, onError));
    }

    public void SignInWithIdToken(string idToken, string refreshToken, string displayName, string email, AuthProviderType provider)
    {
        IdToken = idToken;
        _refreshToken = refreshToken;
        DisplayName = displayName;
        Email = email;
        Provider = provider;

        // Parse UID from JWT
        UserId = ParseUidFromJwt(idToken);

        SaveAuthState();
        StartTokenRefreshLoop();
        OnAuthStateChanged?.Invoke();
        Debug.Log("[Auth] Signed in as " + Provider + " uid=" + UserId + " name=" + DisplayName + " email=" + Email);
        Debug.Log("[Auth] RefreshToken for Editor: " + _refreshToken);
    }

    public void SignOut()
    {
        IdToken = "";
        _refreshToken = "";
        UserId = "";
        DisplayName = "";
        Email = "";
        Provider = AuthProviderType.Guest;

        PlayerPrefs.DeleteKey("AuthRefreshToken");
        PlayerPrefs.DeleteKey("AuthProvider");
        PlayerPrefs.DeleteKey("AuthDisplayName");
        PlayerPrefs.DeleteKey("AuthEmail");
        PlayerPrefs.Save();

        if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = null;

        OnAuthStateChanged?.Invoke();
        Debug.Log("[Auth] Signed out");
    }

    IEnumerator AnonymousAuthCoroutine(Action onSuccess, Action<string> onError)
    {
        string url = AUTH_BASE + ":signUp?key=" + FIREBASE_API_KEY;
        string body = "{\"returnSecureToken\":true}";

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var resp = request.downloadHandler.text;
            IdToken = ExtractJsonString(resp, "idToken");
            _refreshToken = ExtractJsonString(resp, "refreshToken");
            UserId = ExtractJsonString(resp, "localId");
            Provider = AuthProviderType.Guest;
            DisplayName = LeaderboardManager.GetPlayerName();

            SaveAuthState();
            StartTokenRefreshLoop();
            OnAuthStateChanged?.Invoke();
            Debug.Log("[Auth] Anonymous sign-in uid=" + UserId);
            onSuccess?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Auth] Anonymous sign-in failed: " + request.error);
            onError?.Invoke(request.error);
        }

        request.Dispose();
    }

    IEnumerator RefreshTokenCoroutine(Action onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(_refreshToken)) yield break;

        string url = TOKEN_BASE + "?key=" + FIREBASE_API_KEY;
        string body = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(_refreshToken);

        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var resp = request.downloadHandler.text;
            IdToken = ExtractJsonString(resp, "id_token");
            _refreshToken = ExtractJsonString(resp, "refresh_token");
            UserId = ExtractJsonString(resp, "user_id");
            DisplayName = PlayerPrefs.GetString("AuthDisplayName", LeaderboardManager.GetPlayerName());
            Email = PlayerPrefs.GetString("AuthEmail", "");

            SaveAuthState();
            StartTokenRefreshLoop();
            OnAuthStateChanged?.Invoke();
            Debug.Log("[Auth] Token refreshed uid=" + UserId);
            onSuccess?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Auth] Token refresh failed: " + request.error + " — session expired");
            SignOut();
            OnAuthSessionExpired?.Invoke();
            onError?.Invoke(request.error);
        }

        request.Dispose();
    }

    public void EditorDevSignIn(Action onSuccess = null, Action<string> onError = null)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(editorDevRefreshToken))
        {
            Debug.LogWarning("[Auth] No editorDevRefreshToken set in Inspector");
            onError?.Invoke("No dev refresh token");
            return;
        }
        _refreshToken = editorDevRefreshToken;
        Provider = AuthProviderType.Google;
        StartCoroutine(RefreshTokenCoroutine(onSuccess, onError));
#else
        onError?.Invoke("EditorDevSignIn only works in Editor");
#endif
    }

    public void RefreshToken(Action onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(RefreshTokenCoroutine(onSuccess, onError));
    }

    void StartTokenRefreshLoop()
    {
        if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = StartCoroutine(TokenRefreshLoop());
    }

    IEnumerator TokenRefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(TOKEN_REFRESH_INTERVAL);
            if (!string.IsNullOrEmpty(_refreshToken))
                yield return RefreshTokenCoroutine(null, null);
            else
                yield break;
        }
    }

    void SaveAuthState()
    {
        PlayerPrefs.SetString("AuthRefreshToken", _refreshToken);
        PlayerPrefs.SetString("AuthProvider", Provider.ToString());
        PlayerPrefs.SetString("AuthDisplayName", DisplayName);
        PlayerPrefs.SetString("AuthEmail", Email);
        PlayerPrefs.Save();
    }

    static string ParseUidFromJwt(string jwt)
    {
        if (string.IsNullOrEmpty(jwt)) return "";
        var parts = jwt.Split('.');
        if (parts.Length < 2) return "";
        string payload = parts[1];
        // Pad base64
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }
        payload = payload.Replace('-', '+').Replace('_', '/');
        try
        {
            byte[] bytes = Convert.FromBase64String(payload);
            string json = Encoding.UTF8.GetString(bytes);
            return ExtractJsonString(json, "user_id");
        }
        catch { return ""; }
    }

    static string ExtractJsonString(string json, string key)
    {
        string pattern = "\"" + key + "\":\"";
        int idx = json.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0)
        {
            // Try with space after colon
            pattern = "\"" + key + "\": \"";
            idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return "";
        }
        idx += pattern.Length;
        int end = json.IndexOf("\"", idx, StringComparison.Ordinal);
        return end > idx ? json.Substring(idx, end - idx) : "";
    }
}
