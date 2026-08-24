using System.Windows.Input;
using Calegrafia.App.Services;

namespace Calegrafia.App.ViewModels.Auth;

/// <summary>
/// ViewModel for MfaPage.
/// Receives Email and Senha via Shell QueryProperty after LoginPage detects MFA requirement.
/// </summary>
[QueryProperty(nameof(Email), "Email")]
[QueryProperty(nameof(Senha), "Senha")]
public sealed class MfaViewModel : ViewModelBase
{
    private readonly IAuthApiService _authApi;

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, Uri.UnescapeDataString(value ?? string.Empty));
    }

    private string _senha = string.Empty;
    public string Senha
    {
        get => _senha;
        set => SetProperty(ref _senha, Uri.UnescapeDataString(value ?? string.Empty));
    }

    private string _codigo = string.Empty;
    public string Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
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

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    public ICommand VerificarCommand { get; }
    public ICommand ResetMfaCommand { get; }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public MfaViewModel(IAuthApiService authApi)
    {
        _authApi = authApi;

        VerificarCommand = new Command(
            execute: async () => await ExecuteVerificarAsync(),
            canExecute: () => !IsBusy);

        ResetMfaCommand = new Command(
            execute: async () => await ExecuteResetMfaAsync(),
            canExecute: () => !IsBusy);
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    private async Task ExecuteVerificarAsync()
    {
        if (IsBusy) return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Codigo) || Codigo.Trim().Length != 6)
        {
            ErrorMessage = "Informe o código de 6 dígitos do seu autenticador.";
            return;
        }

        IsBusy = true;
        ((Command)VerificarCommand).ChangeCanExecute();
        ((Command)ResetMfaCommand).ChangeCanExecute();

        try
        {
            var result = await _authApi.LoginAsync(Email, Senha, Codigo.Trim());

            if (result is null || string.IsNullOrEmpty(result.AccessToken))
            {
                ErrorMessage = "Código inválido ou expirado. Tente novamente.";
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
            ((Command)VerificarCommand).ChangeCanExecute();
            ((Command)ResetMfaCommand).ChangeCanExecute();
        }
    }

    private async Task ExecuteResetMfaAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email))
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro",
                "Não foi possível identificar o email. Volte ao login e tente novamente.",
                "OK");
            return;
        }

        IsBusy = true;
        ((Command)VerificarCommand).ChangeCanExecute();
        ((Command)ResetMfaCommand).ChangeCanExecute();

        try
        {
            var ok = await _authApi.ResetMfaSolicitarAsync(Email);

            if (ok)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Solicitação enviada",
                    "Se esse email estiver cadastrado, você receberá instruções para redefinir o autenticador.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(
                    "Erro",
                    "Não foi possível enviar a solicitação. Tente novamente mais tarde.",
                    "OK");
            }
        }
        finally
        {
            IsBusy = false;
            ((Command)VerificarCommand).ChangeCanExecute();
            ((Command)ResetMfaCommand).ChangeCanExecute();
        }
    }
}
