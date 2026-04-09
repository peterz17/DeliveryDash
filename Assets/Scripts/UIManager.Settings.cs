using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UIManager
{
    public void ShowSettingsScreen()
    {
        if (settingsScreen != null) settingsScreen.SetActive(true);
        UpdateGoogleLinkStatus();
    }

    public void HideSettingsScreen()
    {
        if (settingsScreen != null) settingsScreen.SetActive(false);
    }

    void UpdateGoogleLinkStatus()
    {
        bool isGoogle = AuthManager.Instance != null && AuthManager.Instance.Provider == AuthProviderType.Google;

        if (googleLinkButton != null)
        {
            googleLinkButton.interactable = !isGoogle;
            var btnText = googleLinkButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = isGoogle
                    ? LocalizationManager.L("settings_google_linked", "Google Account Linked")
                    : LocalizationManager.L("settings_google_link", "Link Google Account");
        }

        if (googleLinkStatusText != null)
        {
            if (isGoogle)
            {
                string email = AuthManager.Instance.Email;
                googleLinkStatusText.text = !string.IsNullOrEmpty(email) ? email : AuthManager.Instance.DisplayName;
            }
            else
            {
                googleLinkStatusText.text = "";
            }
        }
    }

    void LinkGoogleAccount()
    {
        if (googleLinkStatusText != null)
            googleLinkStatusText.text = LocalizationManager.L("auth_signing_in", "Signing in...");

        System.Action onSuccess = () =>
        {
            string email = AuthManager.Instance != null ? AuthManager.Instance.Email : "";
            FirestoreUserProfile.CheckProviderAvailable("Google", email, available =>
            {
                if (!available)
                {
                    if (googleLinkStatusText != null)
                        googleLinkStatusText.text = LocalizationManager.L("auth_email_exists",
                            "This Google account is already linked to another player.");
                    AuthManager.Instance.SignOut();
                    return;
                }
                _profileSynced = true;
                FirestoreUserProfile.SyncOnLogin(() => UpdateGoogleLinkStatus());
            });
        };

        System.Action<string> onError = error =>
        {
            if (googleLinkStatusText == null) return;
            if (error != null && error.Contains("cancelled"))
                googleLinkStatusText.text = LocalizationManager.L("auth_cancelled", "Sign-in cancelled.");
            else
                googleLinkStatusText.text = LocalizationManager.L("auth_error", "Sign-in failed. Try again.");
        };

#if UNITY_EDITOR
        if (AuthManager.Instance != null)
            AuthManager.Instance.EditorDevSignIn(() => onSuccess(), error => onError(error));
#elif UNITY_WEBGL
        if (WebGLAuthProvider.Instance != null)
            WebGLAuthProvider.Instance.SignIn((id, rt) => onSuccess(), error => onError(error));
#else
        if (googleLinkStatusText != null)
            googleLinkStatusText.text = LocalizationManager.L("auth_webgl_only", "Google Sign-In is only available in the web version.");
#endif
    }

    void UpdateLanguageButtonText()
    {
        if (languageButtonText == null) return;
        bool isEng = LocalizationManager.Instance == null || LocalizationManager.Instance.CurrentLanguage == LocalizationManager.Language.English;
        languageButtonText.text = isEng
            ? LocalizationManager.L("lang_current_en", "Language: EN")
            : LocalizationManager.L("lang_current_th", "ภาษา: TH");
    }

    void WireSettingsListeners()
    {
        if (settingsScreen == null) return;

        if (zoneLabelOpacitySlider != null)
        {
            zoneLabelOpacitySlider.onValueChanged.RemoveAllListeners();
            zoneLabelOpacitySlider.value = PlayerPrefs.GetFloat("ZoneLabelOpacity", 1f);
            zoneLabelOpacitySlider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat("ZoneLabelOpacity", v);
                ZoneLabelLocalizer.SetGlobalOpacity(v);
            });
            ZoneLabelLocalizer.SetGlobalOpacity(zoneLabelOpacitySlider.value);
        }

        if (fullscreenToggleButton != null)
        {
            fullscreenToggleButton.onClick.RemoveAllListeners();
            fullscreenToggleButton.onClick.AddListener(() => { Screen.fullScreen = !Screen.fullScreen; UpdateFullscreenButtonLabel(); });
            AddClickSound(fullscreenToggleButton);
            UpdateFullscreenButtonLabel();
        }
    }

    void UpdateFullscreenButtonLabel()
    {
        if (fullscreenLabelText == null) return;
        string label = LocalizationManager.L("settings_fullscreen", "Fullscreen");
        fullscreenLabelText.text = Screen.fullScreen ? "[ON] " + label : "[OFF] " + label;
    }
}
