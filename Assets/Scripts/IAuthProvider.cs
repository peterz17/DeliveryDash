using System;

public enum AuthProviderType { Guest, Google, Apple, Steam }

public interface IAuthProvider
{
    AuthProviderType ProviderType { get; }
    void SignIn(Action<string, string> onSuccess, Action<string> onError);
    void SignOut();
}
