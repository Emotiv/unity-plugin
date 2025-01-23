using System;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;
using WpfWebView2;

public class EmotivAuthentication
{
    private string ClientId { get; set; }
    public EmotivAuthentication(string clientId)
    {
        ClientId = clientId;
    }
    public async Task<string> Authorize()
    {
        try
        {
            var requestUri = "emotiv-" + CreateMD5(ClientId) + "://authorize";
            var parameters = new Parameters
            {
                { OidcConstants.AuthorizeRequest.ClientId, ClientId },
                { OidcConstants.AuthorizeRequest.RedirectUri, requestUri },
                {OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code}
            };
            var request = new RequestUrl("https://cerebrum.emotivcloud.com/api/oauth/authorize/");
            var startUrl = request.Create(parameters);
            var browserOptions = new BrowserOptions(startUrl, requestUri)
            {
                Timeout = TimeSpan.FromMinutes(5),
                DisplayMode = DisplayMode.Visible
            };
            var Browser = new WpfEmbeddedBrowser();
            var browserResult = await Browser.InvokeAsync(browserOptions);

            if (browserResult.ResultType == BrowserResultType.Success)
            {
                AuthorizeResponse response = new AuthorizeResponse(browserResult.Response);
                return response.Code ?? string.Empty;
            }
            return string.Empty;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            return string.Empty;
        }
    }

    private static readonly char[] HEX_ARRAY = "0123456789abcdef".ToCharArray();

    private string BytesToHex(byte[] bytes)
    {
        char[] hexChars = new char[bytes.Length * 2];
        for (int j = 0; j < bytes.Length; ++j)
        {
            int v = bytes[j] & 0xFF;
            hexChars[j * 2] = HEX_ARRAY[v >> 4];
            hexChars[j * 2 + 1] = HEX_ARRAY[v & 0x0F];
        }
        return new string(hexChars);
    }

    private string CreateMD5(string input)
    {
        // Use input string to calculate MD5 hash
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            string hash = BytesToHex(hashBytes).ToLower();
            return hash;
        }
    }
}