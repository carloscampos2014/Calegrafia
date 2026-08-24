using Calegrafia.App.ViewModels.Auth;

namespace Calegrafia.App.Views.Auth;

public partial class CadastroPage : ContentPage
{
    public CadastroPage(CadastroViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
