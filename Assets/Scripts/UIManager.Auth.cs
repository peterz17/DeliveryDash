using UnityEngine;
using TMPro;

public partial class UIManager
{
    bool _profileSynced;

    void HandleAuthReturningSession()
    {
        if (_profileSynced) return;
        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated) return;
        _profileSynced = true;
        FirestoreUserProfile.SyncOnLogin(null);
    }

    void HandleSessionExpired()
    {
        ShowLoginScreen();
        if (authStatusText != null)
            authStatusText.text = LocalizationManager.L("auth_session_expired", "Session expired. Please sign in again.");
    }

    public void ShowLoginScreen()
    {
        SetAllScreens(login: true);
        ShowLoginAuthPhase();
    }

    void ShowLoginAuthPhase()
    {
        if (loginAuthPanel != null) loginAuthPanel.SetActive(true);
        if (loginNamePanel != null) loginNamePanel.SetActive(false);
        if (authStatusText != null) authStatusText.text = "";
    }

    void ShowLoginNamePhase()
    {
        if (loginAuthPanel != null) loginAuthPanel.SetActive(false);
        if (loginNamePanel != null) loginNamePanel.SetActive(true);
        if (playerNameInput != null)
        {
            string saved = LeaderboardManager.GetPlayerName();
            playerNameInput.text = saved == "Player" ? "" : saved;
            playerNameInput.ActivateInputField();
        }
    }

    void ConfirmLogin()
    {
        string name = playerNameInput != null ? playerNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) return;
        LeaderboardManager.SetPlayerName(name);

        if (AuthManager.Instance != null && !AuthManager.Instance.IsAuthenticated)
        {
            if (authStatusText != null)
                authStatusText.text = LocalizationManager.L("auth_syncing", "Loading profile...");
            AuthManager.Instance.SignInAnonymous(
                onSuccess: () => { _profileSynced = true; FirestoreUserProfile.SyncOnLogin(() => SetAllScreens(start: true)); },
                onError: _ => SetAllScreens(start: true)
            );
        }
        else
        {
            FirestoreUserProfile.SyncOnLogin(() => SetAllScreens(start: true));
        }
    }

    void GoogleSignIn()
    {
        if (authStatusText != null)
            authStatusText.text = LocalizationManager.L("auth_signing_in", "Signing in...");

#if UNITY_EDITOR
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.EditorDevSignIn(
                () => OnGoogleSignInComplete(),
                error =>
                {
                    if (authStatusText != null)
                        authStatusText.text = LocalizationManager.L("auth_error", "Sign-in failed. Try again.");
                }
            );
        }
#elif UNITY_WEBGL
        Debug.Log("[UI] GoogleSignIn: WebGLAuthProvider.Instance=" + (WebGLAuthProvider.Instance != null));
        if (WebGLAuthProvider.Instance != null)
        {
            WebGLAuthProvider.Instance.SignIn(
                (idToken, refreshToken) => OnGoogleSignInComplete(),
                error =>
                {
                    if (authStatusText == null) return;
                    if (error != null && error.Contains("cancelled"))
                        authStatusText.text = LocalizationManager.L("auth_cancelled", "Sign-in cancelled.");
                    else
                        authStatusText.text = LocalizationManager.L("auth_error", "Sign-in failed. Try again.");
                }
            );
        }
#else
        if (authStatusText != null)
            authStatusText.text = LocalizationManager.L("auth_webgl_only", "Google Sign-In is only available in the web version.");
#endif
    }

    void OnGoogleSignInComplete()
    {
        Debug.Log("[UI] GoogleSignIn SUCCESS");
        _profileSynced = true;
        if (authStatusText != null)
            authStatusText.text = LocalizationManager.L("auth_syncing", "Loading profile...");
        FirestoreUserProfile.SyncOnLogin(() =>
        {
            string savedName = LeaderboardManager.GetPlayerName();
            if (savedName == "Player" || string.IsNullOrEmpty(savedName))
                ShowLoginNamePhase();
            else
                SetAllScreens(start: true);
        });
    }

    void GuestSignIn()
    {
        ShowLoginNamePhase();
    }
}
