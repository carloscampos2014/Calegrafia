using Calegrafia.App.ViewModels.Auth;

namespace Calegrafia.App.Views.Auth;

public partial class RecuperarSenhaPage : ContentPage
{
    public RecuperarSenhaPage(RecuperarSenhaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
