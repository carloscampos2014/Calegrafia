using Calegrafia.App.Views.Auth;

namespace Calegrafia.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Auth routes
        Routing.RegisterRoute("cadastro", typeof(CadastroPage));
        Routing.RegisterRoute("recuperar-senha", typeof(RecuperarSenhaPage));
        Routing.RegisterRoute("mfa", typeof(MfaPage));

        // Stub route for T-18 (profile selection — not yet implemented)
        // Routing.RegisterRoute("selecionar-perfil", typeof(SelecionarPerfilPage));
    }
}
