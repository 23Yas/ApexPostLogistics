using ApexPost.Courier.Services;

namespace ApexPost.Courier.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var token = TokenEditor.Text?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            StatusLabel.Text = "Token is required.";
            return;
        }

        await SecureStorage.SetAsync("auth_token", token);
        await Shell.Current.GoToAsync("//pending");
    }
}
