using System.Windows.Input;
using Calegrafia.App.Services;

namespace Calegrafia.App.ViewModels.Auth;

/// <summary>
/// ViewModel for RecuperarSenhaPage.
/// Always shows success regardless of whether the email exists (security: no enumeration).
/// </summary>
public sealed class RecuperarSenhaViewModel : ViewModelBase
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

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _mensagem = string.Empty;
    public string Mensagem
    {
        get => _mensagem;
        set => SetProperty(ref _mensagem, value);
    }

    private bool _enviado;
    public bool Enviado
    {
        get => _enviado;
        set => SetProperty(ref _enviado, value);
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    public ICommand EnviarCommand { get; }
    public ICommand VoltarCommand { get; }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public RecuperarSenhaViewModel(IAuthApiService authApi)
    {
        _authApi = authApi;

        EnviarCommand = new Command(
            execute: async () => await ExecuteEnviarAsync(),
            canExecute: () => !IsBusy && !Enviado);

        VoltarCommand = new Command(
            async () => await Shell.Current.GoToAsync(".."));
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    private async Task ExecuteEnviarAsync()
    {
        if (IsBusy || Enviado) return;

        Mensagem = string.Empty;

        if (string.IsNullOrWhiteSpace(Email))
        {
            Mensagem = "Informe seu email.";
            return;
        }

        IsBusy = true;
        ((Command)EnviarCommand).ChangeCanExecute();

        try
        {
            // Fire and forget the actual request — always show success.
            await _authApi.RecuperarSenhaAsync(Email);

            Mensagem = "Se esse email estiver cadastrado, você receberá um link de recuperação em breve.";
            Enviado = true;
            ((Command)EnviarCommand).ChangeCanExecute();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
