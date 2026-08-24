using Calegrafia.App.ViewModels.Auth;

namespace Calegrafia.App.Views.Auth;

public partial class MfaPage : ContentPage
{
    public MfaPage(MfaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
