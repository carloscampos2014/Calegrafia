using System.Net;
using System.Windows.Input;
using Calegrafia.App.Services;

namespace Calegrafia.App.ViewModels.Auth;

/// <summary>
/// ViewModel for LoginPage. Handles email/password login and MFA redirect.
/// </summary>
public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthApiService _authApi;

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _senha = string.Empty;
    public string Senha
    {
        get => _senha;
        set => SetProperty(ref _senha, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    public ICommand LoginCommand { get; }
    public ICommand IrParaCadastroCommand { get; }
    public ICommand IrParaRecuperarSenhaCommand { get; }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public LoginViewModel(IAuthApiService authApi)
    {
        _authApi = authApi;

        LoginCommand = new Command(
            execute: async () => await ExecuteLoginAsync(),
            canExecute: () => !IsBusy);

        IrParaCadastroCommand = new Command(
            async () => await Shell.Current.GoToAsync("cadastro"));

        IrParaRecuperarSenhaCommand = new Command(
            async () => await Shell.Current.GoToAsync("recuperar-senha"));
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    private async Task ExecuteLoginAsync()
    {
        if (IsBusy) return;

        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        {
            ErrorMessage = "Informe email e senha.";
            return;
        }

        IsBusy = true;
        ((Command)LoginCommand).ChangeCanExecute();

        try
        {
            var result = await _authApi.LoginAsync(Email, Senha, codigoMfa: null);

            if (result is null)
            {
                ErrorMessage = "Muitas tentativas. Aguarde alguns minutos.";
                return;
            }

            if (result.MfaRequired)
            {
                // Pass credentials via query string so MfaPage can retry with the code.
                var emailEncoded = Uri.EscapeDataString(Email);
                var senhaEncoded = Uri.EscapeDataString(Senha);
                await Shell.Current.GoToAsync($"mfa?Email={emailEncoded}&Senha={senhaEncoded}");
                return;
            }

            if (string.IsNullOrEmpty(result.AccessToken))
            {
                ErrorMessage = "Email ou senha incorretos.";
                return;
            }

            // Persist tokens securely.
            await SecureStorage.Default.SetAsync("access_token", result.AccessToken);
            if (!string.IsNullOrEmpty(result.RefreshToken))
                await SecureStorage.Default.SetAsync("refresh_token", result.RefreshToken);

            // Navigate to profile selection (stub for T-18).
            await Shell.Current.GoToAsync("//selecionar-perfil");
        }
        finally
        {
            IsBusy = false;
            ((Command)LoginCommand).ChangeCanExecute();
        }
    }
}
