using Calegrafia.App.ViewModels.Auth;

namespace Calegrafia.App.Views.Auth;

[QueryProperty(nameof(MensagemSucesso), "MensagemSucesso")]
public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Receives a success message passed via query string from CadastroPage or RecuperarSenhaPage.
    /// </summary>
    public string MensagemSucesso
    {
        set
        {
            if (BindingContext is LoginViewModel vm && !string.IsNullOrWhiteSpace(value))
                vm.SuccessMessage = Uri.UnescapeDataString(value);
        }
    }
}
