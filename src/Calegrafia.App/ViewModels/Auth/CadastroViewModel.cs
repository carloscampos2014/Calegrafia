using System.Windows.Input;
using Calegrafia.App.Services;

namespace Calegrafia.App.ViewModels.Auth;

/// <summary>
/// ViewModel for CadastroPage. Handles new account registration.
/// </summary>
public sealed class CadastroViewModel : ViewModelBase
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

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha
    {
        get => _confirmarSenha;
        set => SetProperty(ref _confirmarSenha, value);
    }

    private bool _aceitouTermos;
    public bool AceitouTermos
    {
        get => _aceitouTermos;
        set => SetProperty(ref _aceitouTermos, value);
    }

    private bool _aceitouPoliticaPrivacidade;
    public bool AceitouPoliticaPrivacidade
    {
        get => _aceitouPoliticaPrivacidade;
        set => SetProperty(ref _aceitouPoliticaPrivacidade, value);
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

    public ICommand CadastrarCommand { get; }
    public ICommand VoltarCommand { get; }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public CadastroViewModel(IAuthApiService authApi)
    {
        _authApi = authApi;

        CadastrarCommand = new Command(
            execute: async () => await ExecuteCadastrarAsync(),
            canExecute: () => !IsBusy);

        VoltarCommand = new Command(
            async () => await Shell.Current.GoToAsync(".."));
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    private async Task ExecuteCadastrarAsync()
    {
        if (IsBusy) return;

        ErrorMessage = string.Empty;

        // Client-side validation
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Informe um email válido.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Senha) || Senha.Length < 6)
        {
            ErrorMessage = "A senha deve ter pelo menos 6 caracteres.";
            return;
        }

        if (Senha != ConfirmarSenha)
        {
            ErrorMessage = "As senhas não coincidem.";
            return;
        }

        if (!AceitouTermos)
        {
            ErrorMessage = "Você precisa aceitar os Termos de Uso.";
            return;
        }

        if (!AceitouPoliticaPrivacidade)
        {
            ErrorMessage = "Você precisa aceitar a Política de Privacidade.";
            return;
        }

        IsBusy = true;
        ((Command)CadastrarCommand).ChangeCanExecute();

        try
        {
            var ok = await _authApi.CadastrarAsync(Email, Senha, AceitouTermos, AceitouPoliticaPrivacidade);

            if (!ok)
            {
                ErrorMessage = "Não foi possível criar a conta. Tente novamente.";
                return;
            }

            // Navigate back to LoginPage passing a success message via query param.
            await Shell.Current.GoToAsync(
                $"..?MensagemSucesso={Uri.EscapeDataString("Verifique seu email para confirmar o cadastro.")}");
        }
        finally
        {
            IsBusy = false;
            ((Command)CadastrarCommand).ChangeCanExecute();
        }
    }
}
