using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebGLAuthProvider : MonoBehaviour, IAuthProvider
{
    public static WebGLAuthProvider Instance { get; private set; }
    public AuthProviderType ProviderType => AuthProviderType.Google;

    Action<string, string> _onSuccess;
    Action<string> _onError;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void FirebaseGoogleSignIn();
    [DllImport("__Internal")] static extern void FirebaseSignOut();
    [DllImport("__Internal")] static extern bool FirebaseIsReady();
#else
    static void FirebaseGoogleSignIn() => Debug.Log("[WebGLAuth] Google Sign-In only works in WebGL build");
    static void FirebaseSignOut() => Debug.Log("[WebGLAuth] Sign-Out only works in WebGL build");
    static bool FirebaseIsReady() => false;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SignIn(Action<string, string> onSuccess, Action<string> onError)
    {
        _onSuccess = onSuccess;
        _onError = onError;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!FirebaseIsReady())
        {
            onError?.Invoke("Firebase not initialized");
            return;
        }
        FirebaseGoogleSignIn();
#else
        onError?.Invoke("Google Sign-In requires WebGL build");
#endif
    }

    public void SignOut()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseSignOut();
#endif
    }

    // Called from JavaScript via SendMessage
    public void OnGoogleSignInSuccess(string json)
    {
        Debug.Log("[WebGLAuth] OnGoogleSignInSuccess called, json length=" + (json != null ? json.Length : 0));
        string idToken = ExtractField(json, "idToken");
        string refreshToken = ExtractField(json, "refreshToken");
        string displayName = ExtractField(json, "displayName");
        string email = ExtractField(json, "email");
        Debug.Log("[WebGLAuth] displayName=" + displayName + " email=" + email + " hasIdToken=" + !string.IsNullOrEmpty(idToken));

        if (AuthManager.Instance != null)
            AuthManager.Instance.SignInWithIdToken(idToken, refreshToken, displayName, email, AuthProviderType.Google);
        else
            Debug.LogWarning("[WebGLAuth] AuthManager.Instance is null!");

        Debug.Log("[WebGLAuth] _onSuccess is " + (_onSuccess != null ? "set" : "NULL"));
        _onSuccess?.Invoke(idToken, refreshToken);
        _onSuccess = null;
        _onError = null;
    }

    // Called from JavaScript via SendMessage
    public void OnGoogleSignInError(string error)
    {
        Debug.LogWarning("[WebGLAuth] Google Sign-In failed: " + error);
        _onError?.Invoke(error);
        _onSuccess = null;
        _onError = null;
    }

    static string ExtractField(string json, string key)
    {
        string pattern = "\"" + key + "\":\"";
        int idx = json.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return "";
        idx += pattern.Length;
        int end = json.IndexOf("\"", idx, StringComparison.Ordinal);
        return end > idx ? json.Substring(idx, end - idx) : "";
    }
}
